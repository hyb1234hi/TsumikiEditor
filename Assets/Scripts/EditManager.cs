﻿using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;

public partial class EditManager : MonoBehaviour
{
	public enum Tool {
		Block, Eraser, Brush, Spuit,
		PointSelector, RoutePath, Model, MetaInfo
	}

	public enum CoordinateSystem {
		LeftHanded,
		RightHanded,
	}

	public static EditManager Instance {get; private set;}

	public List<EditLayer> Layers {get; private set;}
	public EditLayer CurrentLayer {get {return this.Layers[this.currentLayerIndex];}}
	private int currentLayerIndex = 0;
	
	public Selector Selector {get; private set;}
	public Grid Grid {get; private set;}
	public EditCursor Cursor {get; private set;}
	public RoutePath RoutePath {get; private set;}
	public ToolMenu ToolMenu {get; private set;}
	public ModelProperties ModelProperties {get; private set;}
	public MetaInfo MetaInfo {get; private set;}
	public QuitDialog QuitDialog {get; private set;}
	public LayerListView LayerListView {get; private set;}

	private CoordinateSystem coordinateSystem = CoordinateSystem.RightHanded;
	private Tool tool = Tool.Block;
	private string toolBlock = "cube";
	private string toolModel = "tree";
	private int toolChip = 0;
	private List<Command> cmdlist = new List<Command>();
	private int cmdpos = 0;
	private int groupIndex = -1;
	public bool hasUnsavedData {get; private set;}

	private GameObject blockPaletteListView;
	private GameObject texturePaletteListView;
	private GameObject modelPaletteListView;

	public Material BlockMaterial {get; private set;}
	public Material WaterMaterial {get; private set;}
	public Material ModelMaterial {get; private set;}

	void Awake() {
		if (EditManager.Instance) {
			throw new Exception("EditManager is found.");
		}
		EditManager.Instance = this;
		Application.wantsToQuit += WantToQuit;
		this.Layers = new List<EditLayer>();
		
		var gridObj = new GameObject("Grid");
		gridObj.transform.parent = this.transform;
		this.Grid = gridObj.AddComponent<Grid>();
		
		var cursorObj = new GameObject("EditCursor");
		cursorObj.transform.parent = EditManager.Instance.transform;
		this.Cursor = cursorObj.AddComponent<EditCursor>();
		
		var selectionObj = new GameObject("Selector");
		selectionObj.transform.parent = this.transform;
		this.Selector = selectionObj.AddComponent<Selector>();
		
		var routePathObj = new GameObject("RoutePath");
		selectionObj.transform.parent = this.transform;
		this.RoutePath = routePathObj.AddComponent<RoutePath>();
		routePathObj.SetActive(false);

		this.ToolMenu = GameObject.FindObjectOfType<ToolMenu>();
		this.ModelProperties = GameObject.FindObjectOfType<ModelProperties>();
		this.MetaInfo = GameObject.FindObjectOfType<MetaInfo>();
		
		this.QuitDialog = GameObject.FindObjectOfType<QuitDialog>();
		this.QuitDialog.Close();

		this.LayerListView = GameObject.FindObjectOfType<LayerListView>();

		this.BlockMaterial = Resources.Load<Material>("Materials/BlockMaterial");
		this.WaterMaterial = Resources.Load<Material>("Materials/WaterMaterial");
		this.ModelMaterial = Resources.Load<Material>("Materials/ModelMaterial");

		BlockShape.LoadData();
		ModelShape.LoadData();
	}

	bool WantToQuit() {
		if (Application.isEditor) {
			return true;
		}

		if (this.hasUnsavedData) {
			// 未保存の編集がある場合、確認ダイアログを開く
			if (!this.QuitDialog.IsOpened()) {
				this.QuitDialog.Open((result) => {
					if (result) {
						this.hasUnsavedData = false;
						Application.Quit();
					}
				});
			}
			return false;
		}
		return true;
	}
	
	public void OnDataSaved() {
		this.hasUnsavedData = false;
		EditManager.Instance.UpdateWindowTitle();
	}

	public void OnDataDiscarded() {
		this.hasUnsavedData = false;
		EditManager.Instance.UpdateWindowTitle();
	}

	public void OnDataChanged() {
		bool isChanged = !this.hasUnsavedData;
		this.hasUnsavedData = true;
		if (isChanged) EditManager.Instance.UpdateWindowTitle();
	}

	public void UpdateWindowTitle() {
		string filePath = FileManager.currentFilePath;
		string fileName;
		if (String.IsNullOrEmpty(filePath)) {
			fileName = "NewMap";
		} else {
			fileName = Path.GetFileName(filePath);
			if (this.hasUnsavedData) {
				fileName += " *";
			}
		}
		
		WindowControl.SetWindowTitle(fileName + " - " + Application.productName);
	}

	public void OnTextureChanged() {
		var material = this.Layers[0].GetMaterial();
		
		// Albedoテクスチャを設定
		material.SetTexture("_MainTex", TexturePalette.Instance.GetTexture(0));

		var emissionTexture = TexturePalette.Instance.GetTexture(1);
		if (emissionTexture != null) {
			// Emissionテクスチャが存在するときはEmissionを有効に
			material.SetColor("_EmissionColor", Color.white);
			material.SetTexture("_EmissionMap", emissionTexture);
		} else {
			// Emissionテクスチャが存在しないときはEmissionを無効に
			material.SetColor("_EmissionColor", Color.black);
			material.SetTexture("_EmissionMap", null);
		}
	}

	void Start() {
		this.Reset();
		
		this.blockPaletteListView = GameObject.Find("BlockPaletteListView");
		this.texturePaletteListView = GameObject.Find("TexturePaletteListView");
		this.modelPaletteListView = GameObject.Find("ModelPaletteListView");
		this.SetTool(Tool.Block);

		//FileManager.Load("TestData/test02.tkd");
		//FileManager.Load("TestData/test01.tkd");
	}
	
	private void ShowUIView(GameObject obj) {
		var canvasGroup = obj.GetComponent<CanvasGroup>();
		canvasGroup.alpha = 1.0f;
		canvasGroup.interactable = true;
		canvasGroup.blocksRaycasts = true;
	}
	private void HideUIView(GameObject obj) {
		var canvasGroup = obj.GetComponent<CanvasGroup>();
		canvasGroup.alpha = 0.0f;
		canvasGroup.interactable = false;
		canvasGroup.blocksRaycasts = false;
	}

	public void SetTool(Tool tool) {
		this.tool = tool;

		HideUIView(this.blockPaletteListView);
		HideUIView(this.texturePaletteListView);
		HideUIView(this.modelPaletteListView);
		
		// カーソルにツールをセットする
		switch (this.tool) {
		case Tool.Block:
			this.Cursor.SetBlock(this.toolBlock);
			ShowUIView(this.blockPaletteListView);
			break;
		case Tool.Eraser:
		case Tool.PointSelector:
			this.Cursor.SetBlock();
			break;
		case Tool.Brush:
			this.Cursor.SetPanel();
			ShowUIView(this.texturePaletteListView);
			break;
		case Tool.Spuit:
		case Tool.RoutePath:
			this.Cursor.SetPanel();
			break;
		case Tool.Model:
			this.Cursor.SetModel(this.toolModel);
			ShowUIView(this.modelPaletteListView);
			break;
		case Tool.MetaInfo:
			this.Cursor.SetPanel();
			break;
		}
		
		// 選択以外のツールならセレクタをクリアする
		switch (this.tool) {
		case Tool.PointSelector:
			break;
		default:
			this.Selector.ReleaseBlocks();
			this.Selector.Clear();
			break;
		}

		EditManager.Instance.MetaInfo.gameObject.SetActive(false);
		this.RoutePath.isSelected = false;

		if (this.tool == Tool.RoutePath) {
			this.RoutePath.SetEnabled(true);
		} else {
			this.RoutePath.SetEnabled(false);
		}

		this.UpdateCollider();
	}

	public Tool GetTool() {
		return this.tool;
	}

	public void SetToolBlock(string blockName) {
		this.toolBlock = blockName;
		if (this.tool == Tool.Block) {
			this.Cursor.SetBlock(this.toolBlock);
		}
	}
	
	public void SetToolModel(string modelName) {
		this.toolModel = modelName;
		if (this.tool == Tool.Model) {
			this.Cursor.SetModel(this.toolModel);
		}
	}

	public string GetToolBlock() {
		return this.toolBlock;
	}
	
	public void SetToolChip(int chipIndex) {
		this.toolChip = chipIndex;
	}

	public void SetCurrentLayerIndex(int layerIndex) {
		this.currentLayerIndex = layerIndex;
		this.Selector.SetCurrentLayer(this.CurrentLayer);
		this.UpdateCollider();
	}

	public EditLayer FindLayer(string layerName) {
		foreach (var layer in this.Layers) {
			if (layer.gameObject.name == layerName) {
				return layer;
			}
		}
		return this.Layers[0];
	}

	public EditLayer AddLayer(string name, string materialName) {
		var obj = new GameObject(name);
		var layer = obj.AddComponent<EditLayer>();
		layer.SubmeshEnabled = true;
		layer.SetMaterial(materialName);
		this.Layers.Add(layer);
		this.LayerListView.UpdateView();
		return layer;
	}

	public void RemoveLayer(EditLayer layer) {
		this.Layers.Remove(layer);
		GameObject.Destroy(layer.gameObject);
		this.LayerListView.UpdateView();
	}

	public void Clear() {
		foreach (var layer in this.Layers) {
			GameObject.Destroy(layer.gameObject);
		}
		this.Layers.Clear();
		this.Selector.Clear();
	
		this.cmdlist.Clear();
		this.cmdpos = 0;
	}

	public void Reset() {
		this.Clear();
		FileManager.Reset();
		this.AddLayer("Layer-Blocks", "BlockMaterial");
		this.AddLayer("Layer-Water", "WaterMaterial");
		this.OnDataDiscarded();
	}
	
	public bool IsLeftHanded() {
		return this.coordinateSystem == CoordinateSystem.LeftHanded;
	}
	public bool IsRightHanded() {
		return this.coordinateSystem == CoordinateSystem.RightHanded;
	}
	public Vector3 ToWorldCoordinate(Vector3 position) {
		if (this.IsRightHanded()) {
			position.z = -position.z;
			return position;
		} else {
			return position;
		}
	}

	private void UpdateCollider() {
		if (this.tool == Tool.Brush || this.tool == Tool.Spuit || this.tool == Tool.PointSelector) {
			// 現在のレイヤーしか選択できないようにする
			foreach (var layer in this.Layers) {
				layer.GetComponent<MeshCollider>().enabled = false;
			}
			this.CurrentLayer.GetComponent<MeshCollider>().enabled = true;
		} else {
			foreach (var layer in this.Layers) {
				layer.GetComponent<MeshCollider>().enabled = true;
			}
		}
	}
}
