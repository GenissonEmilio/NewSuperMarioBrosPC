"""Local, invisible libretro integration replay. Never uploads ROM, save, RAM or images.

This probes the native core independently; it is not a test of the C# window or audio device.
Requires Python 3.10+, Pillow. Output contains game data and is excluded from distribution.
"""
import argparse, ctypes as C, json, pathlib, struct, time, zipfile
from PIL import Image


def run(core_path, rom_path, out, script):
    out.mkdir(parents=True, exist_ok=True)
    data_root = out / "test-data"
    data_root.mkdir(exist_ok=True)
    if rom_path.suffix.lower() == ".zip":
        with zipfile.ZipFile(rom_path) as z:
            entries = [e for e in z.infolist() if e.filename.lower().endswith(".nds")]
            assert len(entries) == 1 and entries[0].file_size <= 64 * 1024 * 1024
            rom = z.read(entries[0])
    else:
        rom = rom_path.read_bytes()
    assert rom[12:16] == b"A2DE" and rom[30] == 0
    lib = C.CDLL(str(core_path.resolve()))
    env_type = C.CFUNCTYPE(C.c_bool, C.c_uint, C.c_void_p)
    video_type = C.CFUNCTYPE(None, C.c_void_p, C.c_uint, C.c_uint, C.c_size_t)
    batch_type = C.CFUNCTYPE(C.c_size_t, C.c_void_p, C.c_size_t)
    input_type = C.CFUNCTYPE(C.c_int16, C.c_uint, C.c_uint, C.c_uint, C.c_uint)
    poll_type = C.CFUNCTYPE(None)
    audio_type = C.CFUNCTYPE(None, C.c_int16, C.c_int16)
    strings = {}

    def string(s):
        if s not in strings:
            strings[s] = C.create_string_buffer(s.encode())
        return C.addressof(strings[s])

    variables = {
        "melonds_console_mode": "DS", "melonds_boot_directly": "enabled",
        "melonds_use_fw_settings": "disabled", "melonds_language": "English",
        "melonds_screen_layout": "Top/Bottom", "melonds_screen_gap": "0",
        "melonds_touch_mode": "Touch", "melonds_opengl_renderer": "disabled",
        "melonds_threaded_renderer": "enabled", "melonds_jit_enable": "enabled",
        "melonds_jit_block_size": "32", "melonds_audio_interpolation": "Linear",
    }

    @env_type
    def env(command, p):
        cmd = command & 0xffff
        if cmd in (9, 31):
            C.cast(p, C.POINTER(C.c_void_p))[0] = string(str(data_root.resolve()))
            return True
        if cmd == 10:
            return C.cast(p, C.POINTER(C.c_int))[0] == 1
        if cmd == 15:
            ptrs = C.cast(p, C.POINTER(C.c_void_p))
            key = C.string_at(ptrs[0]).decode()
            value = variables.get(key)
            ptrs[1] = string(value) if value else None
            return value is not None
        if cmd in (3, 17, 18):
            C.cast(p, C.POINTER(C.c_bool))[0] = False
            return True
        if cmd == 47:
            C.cast(p, C.POINTER(C.c_void_p))[0] = string("Player")
            return True
        if cmd == 51:
            return True
        if cmd == 52:
            C.cast(p, C.POINTER(C.c_uint))[0] = 0
            return True
        return cmd in (11, 16, 35, 37, 53, 54, 55, 65, 67, 69)

    last_image = None
    audio_frames = 0
    touch = None
    joypad = 0

    @video_type
    def video(data, width, height, pitch):
        nonlocal last_image
        assert width == 256 and height == 384, (width, height)
        if data:
            raw = C.string_at(data, pitch * height)
            last_image = Image.frombytes("RGB", (width, height), raw, "raw", "BGRX", pitch)

    @batch_type
    def batch(data, frames):
        nonlocal audio_frames
        audio_frames += frames
        return frames

    @input_type
    def input_state(port, device, index, key):
        if port:
            return 0
        if device == 1:
            return C.c_int16(joypad).value if key == 256 else (joypad >> key) & 1
        if device == 6 and touch:
            if key == 0:
                return int((touch[0] + .5) * 65536 / 256 - 32768)
            if key == 1:
                return int((touch[1] + 192 + .5) * 65536 / 384 - 32768)
            if key == 2:
                return 1
        return 0

    poll = poll_type(lambda: None)
    audio = audio_type(lambda a, b: None)
    for name, callback in [("environment", env), ("video_refresh", video), ("audio_sample", audio),
                           ("audio_sample_batch", batch), ("input_poll", poll), ("input_state", input_state)]:
        getattr(lib, "retro_set_" + name)(callback)
    lib.retro_get_memory_data.restype = C.c_void_p
    lib.retro_serialize_size.restype = C.c_size_t
    lib.retro_serialize.argtypes = [C.c_void_p, C.c_size_t]
    lib.retro_serialize.restype = C.c_bool
    lib.retro_unserialize.argtypes = [C.c_void_p, C.c_size_t]
    lib.retro_unserialize.restype = C.c_bool

    class GameInfo(C.Structure):
        _fields_ = [("path", C.c_void_p), ("data", C.c_void_p), ("size", C.c_size_t), ("meta", C.c_void_p)]

    rom_buffer = C.create_string_buffer(rom)
    info = GameInfo(string(str(data_root / "game.nds")), C.addressof(rom_buffer), len(rom), None)
    lib.retro_load_game.argtypes = [C.POINTER(GameInfo)]
    lib.retro_load_game.restype = C.c_bool
    lib.retro_init()
    assert lib.retro_load_game(C.byref(info))
    masks = dict(Up=16, Down=32, Left=64, Right=128, Jump=257, Run=514, Confirm=256, Back=1, GamePause=8)
    results = []
    total = 0
    started = time.monotonic()
    try:
        for step in script:
            if state_path := step.get("loadState"):
                state_bytes = pathlib.Path(state_path).read_bytes()
                state_buffer = C.create_string_buffer(state_bytes)
                assert lib.retro_unserialize(state_buffer, len(state_bytes))
            joypad = 0
            for key in step.get("key", "").split("+"):
                if key:
                    joypad |= masks[key]
            touch = step.get("touch")
            for _ in range(step.get("frames", 0)):
                lib.retro_run()
                total += 1
            ram = C.string_at(lib.retro_get_memory_data(2), 0x400000)
            scene = struct.unpack_from("<H", ram, 0x3bd34)[0]
            if name := step.get("name"):
                assert name.replace("-", "").replace("_", "").isalnum()
                last_image.save(out / (name + "-ds.png"))
                (out / (name + "-ram.bin")).write_bytes(ram)
                entry = dict(name=name, frames=total, scene=scene, joypad=joypad, touch=touch)
                results.append(entry)
                print(json.dumps(entry), flush=True)
        size = lib.retro_serialize_size()
        assert 0 < size <= 32 * 1024 * 1024
        state = C.create_string_buffer(size)
        assert lib.retro_serialize(state, size)
        before = C.string_at(lib.retro_get_memory_data(2), 0x400000)
        joypad = 128
        for _ in range(30):
            lib.retro_run()
        assert lib.retro_unserialize(state, size)
        after = C.string_at(lib.retro_get_memory_data(2), 0x400000)
        (out / "snapshot.state").write_bytes(state.raw)
        report = dict(success=before == after and audio_frames > 1000, desktopInput=False,
                      visibleWindows=False, scope="native core only; C# UI verified separately",
                      snapshotRamRoundtrip=before == after, audioFrames=audio_frames,
                      elapsedSeconds=time.monotonic() - started, results=results)
        (out / "verification.json").write_text(json.dumps(report, indent=2))
        assert report["success"]
    finally:
        lib.retro_unload_game()
        lib.retro_deinit()


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("core", type=pathlib.Path)
    parser.add_argument("rom", type=pathlib.Path)
    parser.add_argument("output", type=pathlib.Path)
    parser.add_argument("script", type=pathlib.Path)
    args = parser.parse_args()
    run(args.core, args.rom, args.output, json.loads(args.script.read_text()))
