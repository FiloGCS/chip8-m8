using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

public class Chip8 : MonoBehaviour{

	public bool DEBUG = false;

	ushort opcode;
	public byte[] memory;
	public byte[] v;
	public ushort I;
	public ushort pc;
	public bool[] gfx;
	byte delay_timer;
	byte sound_timer;
	public ushort[] stack;
	public ushort sp;
	public byte[] key;
	private byte[] prevKey;
	public bool drawFlag = false;
	byte[] chip8_fontset;

	public void init () {
		pc = 0x200;
		opcode = 0;
		I = 0;
		sp = 0;

		memory = new byte[4096];
		v = new byte[16];
		gfx = new bool[64*32];
		stack = new ushort[16];
		key = new byte[16];
		prevKey = key;
		chip8_fontset = new byte[80] { 
			0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
			0x20, 0x60, 0x20, 0x20, 0x70, // 1
			0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
			0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
			0x90, 0x90, 0xF0, 0x10, 0x10, // 4
			0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
			0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
			0xF0, 0x10, 0x20, 0x40, 0x40, // 7
			0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
			0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
			0xF0, 0x90, 0xF0, 0x90, 0x90, // A
			0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
			0xF0, 0x80, 0x80, 0x80, 0xF0, // C
			0xE0, 0x90, 0x90, 0x90, 0xE0, // D
			0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
			0xF0, 0x80, 0xF0, 0x80, 0x80  // F
		};
	}

	public void loadGame(string gameName){
		print ("Loading " + gameName + "...");
		using (FileStream fs = new FileStream (gameName, FileMode.Open, FileAccess.Read)) {
			using (BinaryReader r = new BinaryReader(fs)){
				int i = 0;
				while(r.BaseStream.Position != r.BaseStream.Length){
					memory[512+i] = r.ReadByte ();
					i++;
				}
			}
		}
		for (int i = 0; i < 80; i++) {
			memory [i] = chip8_fontset [i];
		}
	}

	public void emulateCycle(){
		//Fetch Opcode
		opcode = (ushort)((memory[pc] << 8) | memory [pc + 1]);
		if(DEBUG)print("Fetch op: " + opcode.ToString("X"));

		//Decoding the opcode
		switch (opcode & 0xF000) {
		case 0x0000: //0---
			switch (opcode) {

			case 0x00E0: //00E0 Clears the screen
				if(DEBUG)print("00E0: Clear Screen");
				gfx = new bool[64 * 32];
				drawFlag = true;
				pc += 2;
				break;

			case 0x00EE: //00EE Returns from a subroutine
				pc = (ushort)(stack[--sp] + 2);
				if(DEBUG)print("00EE: Subroutine retourning to " + pc + " (sp=" + sp);
				break;

			case 0x0000:
				gfx = new bool[64*32];
				drawFlag = true;
				pc += 2;
				break;
			default: //0NNN
				print("[ERROR] RCA 1802 is not implemented yet");
				break;
			}
			break;
		case 0x1000: //1NNN Jumps to adress NNN
			if(DEBUG)print("1NNN: Jump to address NNN");
			pc = (ushort)(opcode & 0x0FFF);
			break;

		case 0x2000: //2NNN Calls subroutine at NNN
			stack [sp] = pc;
			if(DEBUG)print("2NNN: Call subroutine at NNN from pc = " + pc + " (sp=" + sp);
			sp++;
			pc = (ushort)(opcode & 0x0FFF);
			break;

		case 0x3000: //3XNN Skips the next instruction if VX == NN
			if(DEBUG)print("3XNN: If(VX!=NN)");
			if (v [(opcode & 0x0F00) >> 8] == (opcode & 0x00FF)) {
				pc += 4;
			} else {
				pc += 2;
			}
			break;

		case 0x4000: //4XNN Skips the next instruction if VX != NN
			if(DEBUG)print("4XNN: If(VX==NN)");
			if (v [(opcode & 0x0F00) >> 8] != (opcode & 0x00FF)) {
				pc += 4;
			} else {
				pc += 2;
			}
			break;

		case 0x5000: //5XY0 Skips the next instruction if VX equals VY
			if(DEBUG)print("5XY0: If(VX!=VY)");
			if (v [(opcode & 0x0F00) >> 8] == v[(opcode&0x00F0)>>4]){
				pc += 4;
			} else {
				pc += 2;
			}
			break;

		case 0x6000: //6XNN Sets VX to NN
			if(DEBUG)print("6XNN: Vx = NN");
			v [(opcode & 0x0F00) >> 8] = (byte)(opcode & 0x00FF);
			pc += 2;
			break;

		case 0x7000: //7XNN Adds NN to VX
			if(DEBUG)print("7XNN: Vx += NN");
			v[(opcode&0x0F00)>>8] += (byte)(opcode&0x00FF);
			pc+=2;
			break;

		case 0x8000: //8---
			switch (opcode & 0x000F) {

			case 0x0000: //8XY0 Sets VX to the value of VY
				if(DEBUG)print("8XY0: Vx = Vy");
				v [(opcode % 0x0F00) >> 8] = (byte)(v [(opcode & 0x00F0) >> 4]);
				pc += 2;
				break;

			case 0x0001: //8XY1 Sets VX to (VX OR VY)
				if(DEBUG)print("8XY1: Vx = Vx | Vy");
				v [(opcode & 0x0F00) >> 8] = (byte)(v [(opcode & 0x0F00) >> 8] | v [(opcode & 0x00F0) >> 4]);
				pc += 2;
				break;

			case 0x0002: //8XY2 Sets VX to (VX AND VY)
				if(DEBUG)print("8XY2: Vx = Vx & Vy");
				v [(opcode & 0x0F00) >> 8] = (byte)(v [(opcode & 0x0F00) >> 8] & v [(opcode & 0x00F0) >> 4]);
				pc += 2;
				break;

			case 0x0003: //8XY3 Sets VX to (VX XOR VY)
				if(DEBUG)print("8XY3: Vx = Vx XOR Vy");
				v [(opcode & 0x0F00) >> 8] = (byte)(v [(opcode & 0x0F00) >> 8] ^ v [(opcode & 0x00F0) >> 4]);
				pc += 2;
				break;

			case 0x0004: //8XY4 Adds VY to VX. VF is set to 1 when there's a carry, and 0 otherwise
				if(DEBUG)print("8XY4: VF = carry(VY + VX)");
				if (v [(opcode & 0x00F0) >> 4] > (0xFF - v [(opcode & 0x0F00) >> 8])) {
					v [0xF] = 1;
				} else {
					v [0xF] = 0;
				}
				v [(opcode & 0x0F00) >> 8] += v [(opcode & 0x00F0) >> 4];
				pc += 2;
				break;

			case 0x0005: //8XY5 VY is substracted from VX. VF is set to 0 when there's a borrow, and 1 otherwise
				if(DEBUG)print("8XY5: VF = borrow(VX+VY)");
				if (v [(opcode & 0x0F00) >> 8] < v [(opcode & 0x00F0) >> 4]) {
					v [0xF] = 0;
				} else {
					v [0xF] = 1;
				}
				v [(opcode & 0x0F00) >> 8] -= v [(opcode & 0x00F0) >> 4];
				pc += 2;
				break;

			case 0x0006: //8XY6 Shifts VX right by one. VF is set to the value shifted out this way
				if(DEBUG)print("8XY0: VF = carry(VY+VX)");
				byte temp = (byte)((opcode & 0x0F00) >> 8);
				v [0xF] = (byte)(v [temp] & 0x0001);
				v [temp] = (byte)(v [temp] >> 1);
				pc += 2;
				break;

			case 0x0007: //8XY7 Sets VX to VY minus VX. VF is set to 0 when there's a borrow, and 1 otherwise
				if (v [(opcode & 0x0F00) >> 8] > v [(opcode & 0x00F0) >> 4]) {
					v [0xF] = 0;
				} else {
					v [0xF] = 1;
				}
				v [(opcode & 0x0F00) >> 8] = (byte)(v [(opcode & 0x00F0) >> 4] - v [(opcode & 0x0F00) >> 8]);
				pc += 2;
				break;

			case 0x000E: //8XYE Shifts VX left by one. VF is set to the value shifted out this way
				byte temp2 = (byte)((opcode & 0x0F00) >> 8);
				v [0xF] = (byte)(v [temp2] & 0xA000);
				v [temp2] = (byte)(v [temp2] << 1);
				pc += 2;
				break;
			default:
				print ("[ERROR] - Unknown opcode [0x8000]: 0x" + opcode.ToString ("X"));
				break;
			}
			break;

		case 0x9000: //9XY0 Skips the next instruction if VX doesn't equal VY
			if (v [(opcode & 0x0F00) >> 8] != v [(opcode & 0x00F0) >> 4]) {
				pc += 4;
			} else {
				pc += 2;
			}
			break;

		case 0xA000: //ANNN Sets I to adress NNN
			if(DEBUG)print("ANNN: I = NNN");
			I = (ushort)(opcode & 0x0FFF);
			pc += 2;
			break;

		case 0xB000: //BNNN Jumps to the address NNN + V0
			pc = (ushort)((opcode&0x0FFF) + v[0x0]);
			break;

		case 0xC000: //CXNN Setx VX to the result of a bitwise AND operation on a random number (0-255) and NN
			System.Random rnd = new System.Random ();
			v [(opcode & 0x0F00) >> 8] = (byte)(rnd.Next (0, 255) & (opcode & 0x00FF));
			pc += 2;
			break;

		case 0xD000: //DXYN Draw
			if (DEBUG)
				print ("DXYN: Draw");
			/* BUGGY!
			ushort x = v [(opcode & 0x0F00) >> 8];
			ushort y = v [(opcode & 0x00F0) >> 4];
			ushort height = (ushort)(opcode & 0x000F);
			ushort pixel;
			v [0xF] = 0;
			for (int yline = 0; yline < height; yline++) {
				pixel = memory [I + yline];
				for (int xline = 0; xline < 8; xline++) {
					if ((pixel & (0x80 >> xline)) != 0) {
						if (gfx [(x + xline + ((y + yline) * 64))] == true) {
							v [0xF] = 1;
						}
						gfx [x + xline + ((y + yline) * 64)] ^= true;
					}
				}
			}
			*/
			ushort xPos = (ushort)(v [(opcode & 0x0F00) >> 8]);
			ushort yPos = (ushort)(v [(opcode & 0x00F0) >> 4]);
			ushort height = (ushort)(opcode & 0x000F);
			ushort screenPosX;
			ushort screenPosY;
			ushort row;
			for (int y = 0; y < height; y++) {
				row = memory [I + y];
				for (int x = 0; x < 8; x++) {
					if((row & (0x80 >> x))!=0){
						//BUG!
						int screenPos = xPos + x + (yPos + y) * 64;
						if (gfx [screenPos]) {
							v [0xF] = 1;
							gfx [screenPos] = false;
						} else {
							v [0xF] = 0;
							gfx [screenPos] = true;
						}
					}
				}
			}

			drawFlag = true;
			pc += 2;
			break;

		case 0xE000: //E---
			switch (opcode & 0x00FF) {
			case 0x009E: //EX9E Skips the next instruction if the key stored in VX is pressed
				if (key [v [(opcode & 0x0F00) >> 8]] != 0) {
					pc += 4;
				} else {
					pc += 2;
				}
				break;
			case 0x00A1: //EXA1 Skips the next instruction if the key stored in VX is not pressed
				if (key [v [(opcode & 0x0F00) >> 8]] == 0) {
					pc += 4;
				} else {
					pc += 2;
				}
				break;
			default:
				print ("[ERROR] - Unknown opcode [0xE000]: 0x" + opcode.ToString ("X"));
				break;
			}
			break;

		case 0xF000: //F---
			switch (opcode & 0x00FF) {
			case 0x0007: //FX07 Sets VX to the value of the delay timer
				v [(opcode & 0x0F00) >> 8] = delay_timer;
				pc += 2;
				break;

			case 0x000A: //FX0A A key press is awaited, and then stored in VX (Halt until next key event)
				for (int i = 0; i < 16; i++) {
					if (prevKey [i] == 0 && key [i] != 0) {
						v [(opcode & 0x0F00) >> 8] = (byte)i;
						pc += 2;
						break;
					}
				}
				break;

			case 0x0015: //FX15 Sets the delay timer to VX
				delay_timer = v[(opcode&0x0F00)>>8];
				pc += 2;
				break;

			case 0x0018: //FX18 Sets the sound timer to VX
				sound_timer = v[(opcode&0x0F00)>>8];
				pc += 2;
				break;

			case 0x001E: //FX1E Adds VX to I
				I += v [(opcode & 0x0F00) >> 8];
				pc += 2;
				break;

			case 0x0029: //FX29 Sets I to the location of the sprite for the character in VX
				if(DEBUG)print("FX29: I = the location of the sprite for the character in VX");
				I = (ushort) (v[(opcode & 0x0F00) >> 8] * 5);
				pc += 2;
				break;

			case 0x0033: //FX33 Stores the Binary-coded representation of VX at the addresses I, I+1 and I+2
				if(DEBUG)print("FX33: Stores the binary representation of VX at I, I+1 and I+2");
				byte temp = v[(opcode & 0x0F00) >> 8];
				memory [I]     = (byte)(temp / 100);
				memory [I + 1] = (byte)((temp / 10) % 10);
				memory [I + 2] = (byte)((temp % 100) % 10);
				pc += 2;
				break;

			case 0x0055: //FX55 Stores V0 to VX (included) in memory starting at address I
				for (int i = 0; i <= ((opcode & 0x0F00) >> 8); i++) {
					memory [I + i] = v [i];
				}
				pc += 2;
				break;

			case 0x0065: //FX65 Fills V0 to VX (included) with values from memory starting at address I
				if(DEBUG)print("FX65: Fills V0 to VX with values from memory @ I");
				for (int i = 0; i <= ((opcode & 0x0F00) >> 8); i++) {
					v [i] = memory [I + i];
				}
				pc += 2;
				break;
			default:
				print ("[ERROR] - Unknown opcode [0xFX00]: 0x" + opcode.ToString ("X"));
				break;
			}
			break;
		default:
			print ("[ERROR] - Unknown opcode 0x" + opcode.ToString ("X"));
			break;
		}

		//Update timers
		if (delay_timer > 0) {
			delay_timer--;
		}
		if (sound_timer > 0) {
			sound_timer--;
		}
		if (sound_timer == 1) {
			if (this.GetComponent<AudioSource> () != null) {
				this.GetComponent<AudioSource> ().Play ();
			} else {
				print ("BEEP! (AudioSource not found)");
			}
			sound_timer--;
		}

		//Update key comparator
		prevKey = key;
	}
}
