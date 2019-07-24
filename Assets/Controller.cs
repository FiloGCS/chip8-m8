using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour {
	public Chip8 chip;
	public Material screenMat;
	public Texture2D output;
	public float FREQ = 30;

	private bool emulating = false;
	private float countdown = 0;
	private bool debugStepPressed = false;
	void Start () {
		chip.init ();
		emulate ();
	}

	public void emulate(){
		//Set up render system and register input callbacks
		output = new Texture2D(64,32);
		output.filterMode = FilterMode.Point;
		output.wrapMode = TextureWrapMode.Clamp;
		screenMat.mainTexture = output;

		//Load the game into the memory
		chip.loadGame ("Assets/games/MAZE");

		//Launch emulation loop
		emulating = true;
	}

	void Update(){
		//Read input
		chip.key [0] = Input.GetKey (KeyCode.X) ? (byte)1 : (byte)0;
		chip.key [1] = Input.GetKey (KeyCode.Alpha1) ? (byte)1 : (byte)0;
		chip.key [2] = Input.GetKey (KeyCode.Alpha2) ? (byte)1 : (byte)0;
		chip.key [3] = Input.GetKey (KeyCode.Alpha3) ? (byte)1 : (byte)0;
		chip.key [4] = Input.GetKey (KeyCode.Q) ? (byte)1 : (byte)0;
		chip.key [5] = Input.GetKey (KeyCode.W) ? (byte)1 : (byte)0;
		chip.key [6] = Input.GetKey (KeyCode.E) ? (byte)1 : (byte)0;
		chip.key [7] = Input.GetKey (KeyCode.A) ? (byte)1 : (byte)0;
		chip.key [8] = Input.GetKey (KeyCode.S) ? (byte)1 : (byte)0;
		chip.key [9] = Input.GetKey (KeyCode.D) ? (byte)1 : (byte)0;
		chip.key [10] = Input.GetKey (KeyCode.Z) ? (byte)1 : (byte)0;
		chip.key [11] = Input.GetKey (KeyCode.C) ? (byte)1 : (byte)0;
		chip.key [12] = Input.GetKey (KeyCode.Alpha4) ? (byte)1 : (byte)0;
		chip.key [13] = Input.GetKey (KeyCode.R) ? (byte)1 : (byte)0;
		chip.key [14] = Input.GetKey (KeyCode.F) ? (byte)1 : (byte)0;
		chip.key [15] = Input.GetKey (KeyCode.V) ? (byte)1 : (byte)0;
		debugStepPressed = Input.GetKeyDown (KeyCode.Space);
		if (Input.GetKey (KeyCode.Escape)) {
			emulating = false;
		}
	}

	void FixedUpdate(){
		if (emulating) {
			if (chip.DEBUG) {
				countdown = 100;
			}
			if (countdown <= 0 || (chip.DEBUG && debugStepPressed)) {
				debugStepPressed = false;
				countdown = 1/FREQ;
				//Emulate one cycle
				chip.emulateCycle ();

				//If the draw flag is set, update the screen
				if (chip.drawFlag) {
					//Update screen
					for (int i = 0; i < 64; i++) {
						for (int j = 0; j < 32; j++) {
							output.SetPixel (i, 31-j, (chip.gfx [i + j * 64]) ? Color.green : Color.black);
						}
					}
					output.Apply ();
					chip.drawFlag = false;
				}
			} else {
				countdown -= Time.fixedDeltaTime;
			}
		}
	}
}
