# GetEndscene

Simple tool to retrieve address of Direct3D Endscene for WoW client version 1.12.1. Endscene itself doesn't have a static offset, but there exists a function at a static offset that calls it. This tool creates a hook at that function's address, jumps to our code to grab the address of the Endscene, then returns to the original location.

Very old implementation but the same idea works for a variety of different games.

Uses Blackmagic (https://github.com/acidburn974/Blackmagic) for memory reading and writing.
