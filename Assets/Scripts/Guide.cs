﻿using UnityEngine;
using System.Collections;

// ガイド表示（選択領域とかカーソルを表示する）
public class Guide : MonoBehaviour
{
	private MeshFilter surfaceMeshFilter;
	private MeshRenderer surfaceRenderer;
	private MeshFilter outlineMeshFilter;
	private MeshRenderer outlineRenderer;
	private bool shouldDestroy = false;
	private GameObject modelObject;

	void Awake() {
		surfaceMeshFilter = this.gameObject.AddComponent<MeshFilter>();
		surfaceRenderer = this.gameObject.AddComponent<MeshRenderer>();
		surfaceRenderer.material = Resources.Load<Material>("Materials/GuideMaterial");

		var outlineObj = new GameObject();
		outlineObj.name = "Outline";
		outlineObj.transform.parent = this.transform;
		outlineMeshFilter = outlineObj.AddComponent<MeshFilter>();
		outlineRenderer = outlineObj.AddComponent<MeshRenderer>();
		outlineRenderer.material = Resources.Load<Material>("Materials/GuideMaterial");
	}

	public void SetColor(Color color1, Color color2) {
		Color surfaceColor1 = color1;
		Color surfaceColor2 = color2;
		surfaceColor1.a *= 0.36f;
		surfaceColor2.a *= 0.36f;
		surfaceRenderer.material.SetColor("_Color1",  surfaceColor1);
		surfaceRenderer.material.SetColor("_Color2",  surfaceColor2);
		outlineRenderer.material.SetColor("_Color1",  color1);
		outlineRenderer.material.SetColor("_Color2",  color2);
	}

	void Reset() {
		// 解放する必要あり
		if (this.shouldDestroy) {
			if (surfaceMeshFilter.sharedMesh) {
				DestroyImmediate(surfaceMeshFilter.sharedMesh);
			}
			if (outlineMeshFilter.sharedMesh) {
				DestroyImmediate(outlineMeshFilter.sharedMesh);
			}
		}
		surfaceMeshFilter.sharedMesh = null;
		outlineMeshFilter.sharedMesh = null;
		if (this.modelObject) {
			GameObject.DestroyImmediate(this.modelObject);
			this.modelObject = null;
		}
	}

	public void SetMesh(Mesh surface, Mesh outline, bool shouldDestroy) {
		this.Reset();
		surfaceMeshFilter.sharedMesh = surface;
		outlineMeshFilter.sharedMesh = outline;
		this.shouldDestroy = shouldDestroy;
	}

	public void SetModel(ModelShape shape) {
		this.Reset();
		GameObject go = this.modelObject = shape.Build(surfaceRenderer.sharedMaterial);
		go.transform.parent = this.gameObject.transform;
		go.transform.localPosition = Vector3.zero;
		go.transform.localScale = Vector3.one;
	}
}
