"""Give every entry of an APK (a zip) the same fixed date, in place.

Godot's Android export stamps each file it writes into the APK with the
time of the export, so two exports of the same project differ. F-Droid's
reproducible build compares its APK with ours byte for byte: both run
this after the export.

The date is the one Android's own build tools use (1981-01-01 01:01:02).
Only the date and time fields change, nothing moves: the alignment Godot
gave the files (native libraries on 16 KiB pages) stays as it is. An APK
that is already signed must not go through this (its signature covers
these bytes): run it on the unsigned export, then sign.

    python3 tools/apk_fix_timestamps.py trampoline.apk
"""

import struct
import sys

DOS_TIME = (1 << 11) | (1 << 5) | (2 >> 1)  # 01:01:02
DOS_DATE = ((1981 - 1980) << 9) | (1 << 5) | 1  # 1981-01-01
EXTENDED_TIMESTAMP = 0x5455  # "UT" extra field: Unix times, set to the same date
UNIX_TIME = 347158862  # 1981-01-01 01:01:02 UTC


def _fix_extra(data, start, length):
    """Set the Unix times of an "UT" extra field, if there is one."""
    pos, end = start, start + length
    while pos + 4 <= end:
        tag, size = struct.unpack_from("<HH", data, pos)
        if tag == EXTENDED_TIMESTAMP:
            flags_end = pos + 4 + 1
            for time_pos in range(flags_end, pos + 4 + size, 4):
                struct.pack_into("<I", data, time_pos, UNIX_TIME)
        pos += 4 + size


def fix(path):
    with open(path, "rb") as f:
        data = bytearray(f.read())
    # The end of central directory record: its signature, searched from the
    # end (a zip comment may follow it).
    eocd = data.rfind(b"PK\x05\x06")
    if eocd < 0:
        raise SystemExit(f"{path}: not a zip file")
    entries, cd_size, cd_offset = struct.unpack_from("<HII", data, eocd + 10)
    if data.find(b"APK Sig Block 42", 0, cd_offset) >= 0:
        raise SystemExit(f"{path}: already signed, fix the unsigned APK then sign it")
    pos = cd_offset
    for _ in range(entries):
        if data[pos: pos + 4] != b"PK\x01\x02":
            raise SystemExit(f"{path}: bad central directory at {pos}")
        struct.pack_into("<HH", data, pos + 12, DOS_TIME, DOS_DATE)
        name_len, extra_len, comment_len = struct.unpack_from("<HHH", data, pos + 28)
        (local,) = struct.unpack_from("<I", data, pos + 42)
        _fix_extra(data, pos + 46 + name_len, extra_len)
        # The file's own (local) header repeats the date.
        if data[local: local + 4] != b"PK\x03\x04":
            raise SystemExit(f"{path}: bad local header at {local}")
        struct.pack_into("<HH", data, local + 10, DOS_TIME, DOS_DATE)
        local_name, local_extra = struct.unpack_from("<HH", data, local + 26)
        _fix_extra(data, local + 30 + local_name, local_extra)
        pos += 46 + name_len + extra_len + comment_len
    with open(path, "wb") as f:
        f.write(data)


if __name__ == "__main__":
    for apk in sys.argv[1:]:
        fix(apk)
