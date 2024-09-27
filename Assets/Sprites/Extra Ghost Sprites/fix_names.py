#!/usr/local/bin/python3
import os
import sys
import argparse
import pathlib

parser = argparse.ArgumentParser(
    "name fixer",
    description="online tools are silly and don't know how to sort numbers",
)
parser.add_argument("filename")
parser.add_argument("--dry-run", action="store_true")
parser.add_argument("-v", "--verbose", action="store_true")

args = parser.parse_args()

target_folder = args.filename
dry = args.dry_run or False
verbose = args.verbose or False

folder = os.getcwd() / pathlib.Path(target_folder)
files = os.scandir(folder)

filtered = [file for file in files if file.is_file() and file.path.endswith("png")]


for file in filtered:
    [num, rest] = file.name.replace("-", "_").split("_")
    [name, filetype] = rest.split(".")

    if not num.isdigit():
        copy = num
        num = name
        name = copy

    alpha = [chr(a) for a in range(ord("a"), ord("z"))][int(num) - 1]

    new_name = f"{name}_{alpha}.{filetype}"

    if dry or verbose:
        print(f"{folder / file.name} -> {folder / new_name}")
    if dry:
        continue

    os.rename(folder / file.name, folder / new_name)
