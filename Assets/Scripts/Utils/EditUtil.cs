﻿using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class EditUtil
{
	public static int PositionToHashCode(Vector3 position) {
		int x = Mathf.RoundToInt(position.x) + 512;
		int y = Mathf.RoundToInt(position.y * 2) + 512;
		int z = Mathf.RoundToInt(position.z) + 512;
		return ((z & 0x3f) << 20) | ((y & 0x3f) << 10) | (x & 0x3f);
	}

	public static readonly Vector3[] panelVertices = new Vector3[] {
		new Vector3(-0.5f, 0.0f,  0.5f),
		new Vector3( 0.5f, 0.0f,  0.5f), 
		new Vector3(-0.5f, 0.0f, -0.5f),
		new Vector3( 0.5f, 0.0f, -0.5f), 
	};
	public static readonly int[] panelQuadIndices = {
		0, 1, 3, 2
	};
	public static readonly int[] panelLinesIndices = {
		0, 1, 2, 3, 0, 2, 1, 3
	};

	public static readonly Vector3[] cubeVertices = new Vector3[] {
		new Vector3(-0.5f,  0.25f,  0.5f),
		new Vector3( 0.5f,  0.25f,  0.5f), 
		new Vector3(-0.5f,  0.25f, -0.5f),
		new Vector3( 0.5f,  0.25f, -0.5f), 
		new Vector3(-0.5f, -0.25f,  0.5f),
		new Vector3( 0.5f, -0.25f,  0.5f), 
		new Vector3(-0.5f, -0.25f, -0.5f),
		new Vector3( 0.5f, -0.25f, -0.5f), 
	};
	public static readonly int[] cubeQuadIndices = {
		3, 7, 6, 2, 0, 4, 5, 1,
		1, 5, 7, 3, 2, 6, 4, 0,
		2, 0, 1, 3, 4, 6, 7, 5,
	};
	public static readonly int[] cubeLineIndices = {
		0, 2, 1, 3, 0, 1, 2, 3,
		4, 5, 6, 7, 4, 6, 5, 7,
		0, 4, 1, 5, 2, 6, 3, 7
	};

	public static void Swap<T>(ref T lhs, ref T rhs) {
		T temp;
		temp = lhs;
		lhs = rhs;
		rhs = temp;
	}
	public static void MinMax(ref float min, ref float max) {
		if (min > max) {
			Swap(ref min, ref max);
		}
	}
	public static void MinMax(ref int min, ref int max) {
		if (min > max) {
			Swap(ref min, ref max);
		}
	}
	public static void MinMaxElements(ref Vector3 min, ref Vector3 max) {
		MinMax(ref min.x, ref max.x);
		MinMax(ref min.y, ref max.y);
		MinMax(ref min.z, ref max.z);
	}
	public static void MinMaxElements(ref Vector3i min, ref Vector3i max) {
		MinMax(ref min.x, ref max.x);
		MinMax(ref min.y, ref max.y);
		MinMax(ref min.z, ref max.z);
	}

	public static Mesh CreateGrid(int cx, int cz) {
		var vertexPos = new List<Vector3>();
		var linesIndices = new List<int>();

		Vector3 half = new Vector3(cx * 0.5f, 0.0f, cz * 0.5f);
		int indexOffset = 0;
		for (int i = 0; i < cx + 1; i++) {
			vertexPos.Add(new Vector3(i - half.x, 0.0f, -half.z));
			vertexPos.Add(new Vector3(i - half.x, 0.0f, +half.z));
			linesIndices.Add(indexOffset + i * 2);
			linesIndices.Add(indexOffset + i * 2 + 1);
		}

		indexOffset += vertexPos.Count;
		for (int i = 0; i < cz + 1; i++) {
			vertexPos.Add(new Vector3(-half.x, 0.0f, i - half.z));
			vertexPos.Add(new Vector3(+half.x, 0.0f, i - half.z));
			linesIndices.Add(indexOffset + i * 2);
			linesIndices.Add(indexOffset + i * 2 + 1);
		}

		Mesh mesh = new Mesh();
		mesh.vertices = vertexPos.ToArray();
		mesh.SetIndices(linesIndices.ToArray(), MeshTopology.Lines, 0);
		return mesh;
	}

	public static Mesh CreatePanelLineMesh() {
		Mesh mesh = new Mesh();
		mesh.vertices = panelVertices;
		mesh.SetIndices(panelLinesIndices, MeshTopology.Lines, 0);
		return mesh;
	}
	
	public static Mesh CreatePanelSurfaceMesh() {
		Mesh mesh = new Mesh();
		mesh.vertices = panelVertices;
		mesh.SetIndices(new int[] {2, 0, 1, 3}, MeshTopology.Quads, 0);
		return mesh;
	}

	public static Mesh CreateBlockLineMesh() {
		Mesh mesh = new Mesh();
		mesh.vertices = cubeVertices;
		mesh.SetIndices(cubeLineIndices, MeshTopology.Lines, 0);
		return mesh;
	}

	public static Mesh CreateBlockSurfaceMesh() {
		Mesh mesh = new Mesh();
		mesh.vertices = cubeVertices;
		mesh.SetIndices(cubeQuadIndices, MeshTopology.Quads, 0);
		return mesh;
	}
	
	// ベクトル方向をブロック方向に変換
	public static BlockDirection VectorToDirection(Vector3 vector) {
		float u = Vector3.Dot(vector, Vector3.up);
		if (u >  0.5f) return BlockDirection.Yplus;
		if (u < -0.5f) return BlockDirection.Yminus;
		float f = Vector3.Dot(vector, EditManager.Instance.ToWorldCoordinate(Vector3.forward));
		if (f >  0.5f) return BlockDirection.Zplus;
		if (f < -0.5f) return BlockDirection.Zminus;
		float r = Vector3.Dot(vector, Vector3.right);
		if (r >  0.5f) return BlockDirection.Xplus;
		if (r < -0.5f) return BlockDirection.Xminus;
		return BlockDirection.Zplus;
	}

	// ブロック方向をY軸回転角に変換
	public static float DirectionToAngle(BlockDirection direction) {
		float angle = 0.0f;
		switch (direction) {
		case BlockDirection.Zplus:	angle =   0.0f;	break;
		case BlockDirection.Xplus:	angle =  90.0f;	break;
		case BlockDirection.Zminus:	angle = 180.0f;	break;
		case BlockDirection.Xminus:	angle = 270.0f;	break;
		}
		return angle;
	}

	public static BlockDirection RotateDirection(BlockDirection direction, int rotation) {
		if (rotation > 0) {
			for (int i = 0; i < rotation; i++) {
				switch (direction) {
				case BlockDirection.Zplus:	direction = BlockDirection.Xplus;	break;
				case BlockDirection.Xplus:	direction = BlockDirection.Zminus;	break;
				case BlockDirection.Zminus:	direction = BlockDirection.Xminus;	break;
				case BlockDirection.Xminus:	direction = BlockDirection.Zplus;	break;
				}
			}
		} else if (rotation < 0) {
			for (int i = 0; i > rotation; i--) {
				switch (direction) {
				case BlockDirection.Zplus:	direction = BlockDirection.Xminus;	break;
				case BlockDirection.Xplus:	direction = BlockDirection.Zplus;	break;
				case BlockDirection.Zminus:	direction = BlockDirection.Xplus;	break;
				case BlockDirection.Xminus:	direction = BlockDirection.Zminus;	break;
				}
			}
		}
		return direction;
	}
	
	public static Vector3 RotatePosition(Vector3 position, BlockDirection direction) {
		switch (direction) {
		case BlockDirection.Zplus:	return position;
		case BlockDirection.Zminus:	return new Vector3(-position.x, position.y, -position.z);
		case BlockDirection.Xplus:	return new Vector3( position.z, position.y, -position.x);
		case BlockDirection.Xminus:	return new Vector3(-position.z, position.y,  position.x);
		}
		return position;
	}

	public static Vector3 RotatePosition(Vector3 position, int rotation) {
		if (rotation > 0) {
			for (int i = 0; i < rotation; i++) {
				position = new Vector3( position.z, position.y, -position.x);
			}
		} else if (rotation < 0) {
			for (int i = 0; i > rotation; i--) {
				position = new Vector3(-position.z, position.y,  position.x);
			}
		}
		return position;
	}
	
	private static readonly int[][] RotatePanelVertexIndexTable = new int[][] {
		new int[] {0, 1, 2, 3}, 
		new int[] {3, 2, 1, 0}, 
		new int[] {2, 0, 3, 1}, 
		new int[] {1, 3, 0, 2}, 
	};
	public static int RotatePanelVertexIndex(int vertexIndex, BlockDirection direction) {
		switch (direction) {
		case BlockDirection.Zplus:	return RotatePanelVertexIndexTable[0][vertexIndex];
		case BlockDirection.Zminus:	return RotatePanelVertexIndexTable[1][vertexIndex];
		case BlockDirection.Xplus:	return RotatePanelVertexIndexTable[2][vertexIndex];
		case BlockDirection.Xminus:	return RotatePanelVertexIndexTable[3][vertexIndex];
		}
		return vertexIndex;
	}
	public static int ReversePanelVertexIndex(int vertexIndex, BlockDirection direction) {
		switch (direction) {
		case BlockDirection.Zplus:	return RotatePanelVertexIndexTable[0][vertexIndex];
		case BlockDirection.Zminus:	return RotatePanelVertexIndexTable[1][vertexIndex];
		case BlockDirection.Xplus:	return RotatePanelVertexIndexTable[3][vertexIndex];
		case BlockDirection.Xminus:	return RotatePanelVertexIndexTable[2][vertexIndex];
		}
		return vertexIndex;
	}

	// スクリーンの上方向と右方向を指すワールド方向を取得
	public static void ScreenDirToWorldDir(out Vector3 up, out Vector3 right) {
		Vector3 upDir = Camera.main.transform.up;
		Vector3 rightDir = Camera.main.transform.right;
		
		Vector3[] worldDir = new Vector3[]{Vector3.up*0.5f, Vector3.down*0.5f, Vector3.right, Vector3.left, Vector3.forward, Vector3.back};
		
		int upDirIndex = -1, rightDirIndex = -1;
		float upDirDot = -1.0f, rightDirDot = -1.0f;
		for (int i = 0; i < worldDir.Length; i++) {
			float dot = Vector3.Dot(upDir, worldDir[i].normalized);
			if (dot > upDirDot) {
				upDirIndex = i;
				upDirDot = dot;
			}
		}
		for (int i = 0; i < worldDir.Length; i++) {
			float dot = Vector3.Dot(rightDir, worldDir[i].normalized);
			if (dot > rightDirDot) {
				rightDirIndex = i;
				rightDirDot = dot;
			}
		}
		
		up = worldDir[upDirIndex];
		right = worldDir[rightDirIndex];
	}

	// ブロック位置を修正する
	public static Vector3 ResolvePosition(Vector3 position) {
		position.x = Mathf.Round(position.x);
		position.y = Mathf.Round(position.y * 2) * 0.5f;
		position.z = Mathf.Round(position.z);
		return position;
	}

	public static Texture2D LoadTextureFromFile(string path) {
		if (!File.Exists(path)) {
			return null;
		}
		byte[] imageData = File.ReadAllBytes(path);
		var texture = new Texture2D(0, 0);
		texture.LoadImage(imageData);
		return texture;
	}
}
