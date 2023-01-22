using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic; 
using System;
using System.IO;
using System.Net; 
using UnityEditor.Animations;

using UnityEngine.UI;
using UnityEngine.Events;
using UnityEditor.Events;
using System.Reflection;
using UnityEditor.Build.Reporting;

public class ModelAndAnimationsImporter: EditorWindow {
	#region Internal
	private static int count = 0;
	public List<AnimatorStateTransition> _animTransition;
	public Dictionary<string,List<AnimatorState>> _animState = new Dictionary<string,List<AnimatorState>>();
	public AnimatorState  idlestate ;
	//Version_2(multiple Folder)
	static List<string> FolderNames;
	static List<string> ModelNames;

	string editingValue;
	string lastFocusedControl;
	static List<string> _modelNameList;

	private class Context {
		public string modelFilePath;
		public string modelFileName;
		public string animsFolderPath;
		public string modelName;

		public string tempFolderPath;
		public Context(string _modelFilePath, string _animsFolderPath) {
			Initialize(_modelFilePath, _animsFolderPath);
		}
		
		private void Initialize(string _modelFilePath, string _animsFolderPath) {
			count++;
			
			modelFilePath   = _modelFilePath;
			modelFileName   = Path.GetFileName(modelFilePath);
			animsFolderPath = _animsFolderPath;
			
			modelName       = Path.GetFileNameWithoutExtension(modelFilePath);
			
			tempFolderPath  = Path.GetTempPath() + Path.DirectorySeparatorChar + "tmpImportModel" + count;
			
			if(Directory.Exists(tempFolderPath))
				Directory.Delete(tempFolderPath,true);
			Directory.CreateDirectory(tempFolderPath);
		}
		
		public Context() {
			string _modelFilePath   = EditorUtility.OpenFilePanel ("Select model file...", "", "FBX");
			string _animsFolderPath = EditorUtility.OpenFolderPanel("Select animations folder...", "", "");
			
			Initialize(_modelFilePath, _animsFolderPath);
		}
	}

	class AnimationClipSettings
	{
		SerializedProperty m_Property;
		
		private SerializedProperty Get (string property) { return m_Property.FindPropertyRelative(property); }
		
		public AnimationClipSettings(SerializedProperty prop) { m_Property = prop; }
		
		public float startTime   { get { return Get("m_StartTime").floatValue; } set { Get("m_StartTime").floatValue = value; } }
		public float stopTime	{ get { return Get("m_StopTime").floatValue; }  set { Get("m_StopTime").floatValue = value; } }
		public float orientationOffsetY { get { return Get("m_OrientationOffsetY").floatValue; } set { Get("m_OrientationOffsetY").floatValue = value; } }
		public float level { get { return Get("m_Level").floatValue; } set { Get("m_Level").floatValue = value; } }
		public float cycleOffset { get { return Get("m_CycleOffset").floatValue; } set { Get("m_CycleOffset").floatValue = value; } }
		
		public bool loopTime { get { return Get("m_LoopTime").boolValue; } set { Get("m_LoopTime").boolValue = value; } }
		public bool loopBlend { get { return Get("m_LoopBlend").boolValue; } set { Get("m_LoopBlend").boolValue = value; } }
		public bool loopBlendOrientation { get { return Get("m_LoopBlendOrientation").boolValue; } set { Get("m_LoopBlendOrientation").boolValue = value; } }
		public bool loopBlendPositionY { get { return Get("m_LoopBlendPositionY").boolValue; } set { Get("m_LoopBlendPositionY").boolValue = value; } }
		public bool loopBlendPositionXZ { get { return Get("m_LoopBlendPositionXZ").boolValue; } set { Get("m_LoopBlendPositionXZ").boolValue = value; } }
		public bool keepOriginalOrientation { get { return Get("m_KeepOriginalOrientation").boolValue; } set { Get("m_KeepOriginalOrientation").boolValue = value; } }
		public bool keepOriginalPositionY { get { return Get("m_KeepOriginalPositionY").boolValue; } set { Get("m_KeepOriginalPositionY").boolValue = value; } }
		public bool keepOriginalPositionXZ { get { return Get("m_KeepOriginalPositionXZ").boolValue; } set { Get("m_KeepOriginalPositionXZ").boolValue = value; } }
		public bool heightFromFeet { get { return Get("m_HeightFromFeet").boolValue; } set { Get("m_HeightFromFeet").boolValue = value; } }
		public bool mirror { get { return Get("m_Mirror").boolValue; } set { Get("m_Mirror").boolValue = value; } }
	}

	private static void DoTempCopy(Context ctx) {
		File.Copy(ctx.modelFilePath, ctx.tempFolderPath + Path.DirectorySeparatorChar + ctx.modelFileName);
		
		foreach(string animFileName in Directory.GetFiles(ctx.animsFolderPath)) {
			/*if(!Path.GetExtension(animFileName).ToLower().Equals("fbx"))
				continue;*/
			if(Path.GetFileName(animFileName).Equals(ctx.modelFileName))
				continue;

			string newFileName = ctx.modelName + "@" + Path.GetFileName(animFileName);
			File.Copy(animFileName, ctx.tempFolderPath + Path.DirectorySeparatorChar + newFileName);
		}
	}
	
	private static void DeletePreviousAssets(Context ctx, string assetFolderPath) {
		foreach(string filePath in Directory.GetFiles(assetFolderPath)) {
			// Only delete existing assets. This preserves any later added assets to a model folder
			if(File.Exists(ctx.tempFolderPath + Path.DirectorySeparatorChar + Path.GetFileName(filePath)))
				AssetDatabase.DeleteAsset(filePath);
		}
	}
	
	private static void ImportAssets(Context ctx) {
		if(!Directory.Exists("Assets" + Path.DirectorySeparatorChar + "Resources"))
			Directory.CreateDirectory("Assets" + Path.DirectorySeparatorChar + "Resources");
		string assetFolderPath = "Assets" + Path.DirectorySeparatorChar + "Resources" + Path.DirectorySeparatorChar +
			ctx.modelName;
		if(!Directory.Exists(assetFolderPath))
			Directory.CreateDirectory(assetFolderPath);
		else
			DeletePreviousAssets(ctx, assetFolderPath);
		
		foreach(string filePath in Directory.GetFiles(ctx.tempFolderPath)) {
			string fileName = Path.GetFileName(filePath);
			
			File.Copy(filePath, assetFolderPath + Path.DirectorySeparatorChar + fileName);
		}
		foreach(string filePath in Directory.GetFiles(assetFolderPath)) {
			AssetDatabase.ImportAsset(filePath);
		}
	}
	
	private static void DeleteTempFolder(Context ctx) {
		Directory.Delete (ctx.tempFolderPath,true);
	}
	
	private static void internalModelImport(Context ctx) {
		if (EditorUtility.DisplayDialog ("Done?", "Have you selected all models?", "Yes", "No")) {
			DoTempCopy (ctx);
			ImportAssets (ctx);
			DeleteTempFolder (ctx);
		} else
			return;
	}
	#endregion
	
	public static void ModelImport(string modelFilePath, string animsFolderPath) {
		Context ctx = new Context(modelFilePath, animsFolderPath);
		internalModelImport(ctx);
	}
	public static void ModelImport(string[] modelFilePaths, string animsFolderPath) {
		foreach(string modelFilePath in modelFilePaths) {
			ModelImport (modelFilePath, animsFolderPath);
		}
	}

//	//Editor Scripting Variables
//	GUIContent ImportModel = new GUIContent("Import Model and Animations");
//	GUIContent BuildAnimations = new GUIContent("BuildAnimations");



	[MenuItem("AnimationImporter/ImporterAnimations")]
	public static void ModelImportGUI() {
		Context ctx = new Context(); // Create context and temp folder
		internalModelImport(ctx);	
	}
	//	[MenuItem("AnimationImporter/ImportAnimations")]
	//	public static void ModelImportBatchGUI() {
	//		string animsFolderPath = EditorUtility.OpenFolderPanel("Select animations folder...", "", "");
	//		List<string> modelFilePathList = new List<string>();
	//		bool done = false;
	//		while(!done) {
	//			string path = EditorUtility.OpenFilePanel("Select model file...", "", "FBX");
	//			if(path == null || path.Length <= 0) {
	//				done = true;
	//				break;
	//			}
	//			modelFilePathList.Add(path);
	//			
	//			if(EditorUtility.DisplayDialog("Done?", "Have you selected all models?", "Yes", "No"))
	//				done = true;
	//		}
	//		string[] modelFilePaths = modelFilePathList.ToArray();
	//		ModelImport(modelFilePaths, animsFolderPath);
	//	}
	//Version_1
	//GUIContent AnimationsFolderLabel = new GUIContent("AnimationsFolder");

	static bool _loadAnimationWindow = false;
	static bool _loadModelWindow = false;

	const string applicationName = "AnimationImporter";
	GUIContent LoadAnimationsLabel = new GUIContent("LoadAnimations");
	[MenuItem("AnimationImporter/LoadAnimations")]
	static void Init ()
	{
		// Show existing open window, or make new one.
		ModelAndAnimationsImporter window = EditorWindow.GetWindow(typeof(ModelAndAnimationsImporter)) as ModelAndAnimationsImporter;
		window.Show();
		FolderNames = new List<string>();
		_loadAnimationWindow = true;
		_loadModelWindow = false;
	}
	
	GUIContent LoadModelLabel = new GUIContent("LoadModels");
	[MenuItem("AnimationImporter/LoadModels")]
	static void LoadModels()
	{
		// Show existing open window, or make new one.
		ModelAndAnimationsImporter window = EditorWindow.GetWindow(typeof(ModelAndAnimationsImporter)) as ModelAndAnimationsImporter;
		window.Show();
		ModelNames = new List<string>();
		_modelNameList = new List<string>();
		_loadAnimationWindow = false;
		_loadModelWindow = true;
	}

	void OnGUI ()
	{
		//		var AnimationsFolder = EditorPrefs.GetString(applicationName + "AnimationsFolder", "");
		//		AnimationsFolder	= EditorGUILayout.TextField(AnimationsFolderLabel, AnimationsFolder);
		//		EditorPrefs.SetString(applicationName + "AnimationsFolder", AnimationsFolder);
		if (_loadAnimationWindow)
		{
			EditorGUILayout.HelpBox("Multiple Animations Loader.\nPress Enter to apply field changes.", MessageType.Info);

			List<string> editedValues = new List<string>();
			string newValue;
			if (FolderNames != null)
			{
				foreach (string val in FolderNames)
				{
					newValue = val;

					if (ShowField("field " + val, ref newValue))
					{
						if (string.IsNullOrEmpty(newValue))
							continue;

						if (FolderNames.IndexOf(newValue) >= 0)
							newValue = val;
					}

					editedValues.Add(newValue);
				}
			}
			newValue = "";

			if (ShowField("new field", ref newValue))
			{
				if (!string.IsNullOrEmpty(newValue) && FolderNames.IndexOf(newValue) < 0)
					editedValues.Add(newValue);
			}

			FolderNames = editedValues;

			EditorGUILayout.BeginHorizontal();

			GUILayout.FlexibleSpace();

			if (GUILayout.Button(LoadAnimationsLabel))
			{
				ResetObjects();
				CreateCanvas();
				_animTransition = new List<AnimatorStateTransition>();
				foreach (string _foldername in FolderNames)
				{
					CreateButtonPanel(_foldername);
					CreateAnimatorController(_foldername);
					LoadAnimations(_foldername);

				}
			}

			EditorGUILayout.EndHorizontal();
		}

		if (_loadModelWindow)
		{
			EditorGUILayout.HelpBox("Multiple Models Loader.\nPress Enter to apply field changes.", MessageType.Info);

			List<string> _nameList = new List<string>();
			string newValue;
			if (ModelNames != null)
			{
				foreach (string val in ModelNames)
				{
					newValue = val;

					if (ShowField("field " + val, ref newValue))
					{
						if (string.IsNullOrEmpty(newValue))
							continue;

						if (ModelNames.IndexOf(newValue) >= 0)
							newValue = val;
					}

					_nameList.Add(newValue);
				}
			}
			newValue = "";

			if (ShowField("new field", ref newValue))
			{
				if (!string.IsNullOrEmpty(newValue) && ModelNames.IndexOf(newValue) < 0)
					_nameList.Add(newValue);
			}

			ModelNames = _nameList;

			EditorGUILayout.BeginHorizontal();

			GUILayout.FlexibleSpace();

			if (GUILayout.Button(LoadModelLabel))
			{
				ResetObjects();
				foreach (string _foldername in ModelNames)
					LoadModelFbx(_foldername);
				CreateModelCanvas();
			}

			EditorGUILayout.EndHorizontal();
		}
	}


	void ResetObjects()
	{
		if(_BTNPanel != null)
			_BTNPanel.Clear();
		if (_modelNameList != null)
			_modelNameList.Clear();
		controller = null;
		rootStateMachine = null;
		stateMachineStand = null;
		if(_animTransition != null)
		_animTransition.Clear ();
		if(_animState != null)
		_animState.Clear ();
		idlestate = null;
		editingValue = null;
		lastFocusedControl = null;
		_count = 0;	
		_basemodel = null; 
		_canvasObj = null;
	}

	bool ShowField(string name, ref string val) {
		GUI.SetNextControlName(name);
		
		if (GUI.GetNameOfFocusedControl() != name) {
			
			if (Event.current.type == EventType.Repaint && string.IsNullOrEmpty(val)) {
				GUIStyle style = new GUIStyle(GUI.skin.textField);
				style.normal.textColor = new Color(0.5f, 0.5f, 0.5f, 0.75f);
				EditorGUILayout.TextField("Enter a new item", style);
			}
			else
				EditorGUILayout.TextField(val);
			
			return false;
		}
		
		//Debug.Log("Focusing " + GUI.GetNameOfFocusedControl());   // Uncomment to show which control has focus.
		
		if (lastFocusedControl != name) {
			lastFocusedControl = name;
			editingValue = val;
		}
		
		bool applying = false;
		
		if (Event.current.isKey) {
			switch (Event.current.keyCode) {
			case KeyCode.Return:
			case KeyCode.KeypadEnter:
				val = editingValue;
				applying = true;
				Event.current.Use();    // Ignore event, otherwise there will be control name conflicts!
				break;
			}
		}
		
		editingValue = EditorGUILayout.TextField(editingValue);    
		return applying;
	}

	//Used for Loading Models for DIsplay

	int _num = 0;
	void LoadModelFbx(string _folderName)
	{
		string mypath = Application.dataPath;
		string resourcepath = string.Concat("/Resources", "/", _folderName, "/");
		mypath = string.Concat(mypath, "/Resources", "/", _folderName);
		DirectoryInfo dir = new DirectoryInfo(mypath);
		FileInfo[] fileinfo = dir.GetFiles("*.fbx");
		if (fileinfo != null && fileinfo.Length > 0)
			foreach (FileInfo file in fileinfo)
				CreateModelFbx(file.Name, resourcepath, _folderName);
	}
	


	GameObject _Modelbasemodel = null;
	void CreateModelFbx(string _filenames, string _path, string _foldername)
	{
			_Modelbasemodel = (GameObject)AssetDatabase.LoadAssetAtPath("Assets" + _path + _filenames, typeof(GameObject));
			GameObject _model = Instantiate(_Modelbasemodel);
			string _name = _filenames.Replace(".fbx", "").Trim();
			_modelNameList.Add(_name);
			_model.name = _name;
		
	}


	//Used For Loading Animations
	int _count = 0;
	void LoadAnimations(string _folderName)
	{
		string mypath = Application.dataPath;
		string resourcepath = string.Concat("/Resources","/",_folderName,"/");
		mypath = string.Concat (mypath, "/Resources","/",_folderName);
		DirectoryInfo dir = new DirectoryInfo(mypath);
		FileInfo[] fileinfo = dir.GetFiles("*.fbx");
		if (fileinfo != null && fileinfo.Length > 0)
		{
			foreach (FileInfo file in fileinfo)
			CreateModel(file.Name, resourcepath,_folderName);


			foreach (FileInfo file in fileinfo){
				if (file.Extension == ".fbx"){
					if(file.Name.Contains("Default")&&file.Name.Contains("@"))	{
						AnimatorState s = CreateAnimationClip (file.Name, resourcepath);
						if(_animState != null && _animState.ContainsKey(_folderName)){
							_animState[_folderName].Add(s); 
						}
						else
						{
							List<AnimatorState> _tempState = new List<AnimatorState>();
							_tempState.Add(s);
						_animState.Add(_folderName,_tempState);
						}
					}
			}
			}
			 
			foreach (FileInfo file in fileinfo)
			{
				if (file.Extension == ".fbx")
				{
					if(!file.Name.Contains("Default")&&file.Name.Contains("@"))
					{
						AnimatorState s = CreateAnimationClip (file.Name, resourcepath);
						if(_animState != null && _animState.ContainsKey(_folderName)){
							_animState[_folderName].Add(s); 
						}
						else
						{
							List<AnimatorState> _tempState = new List<AnimatorState>();
							_tempState.Add(s);
							_animState.Add(_folderName,_tempState);
						}
						_count = 0;
					}
				}
			}
		}
		CreateAnimatorStateTransition(_folderName);
		//CheckANimationstate ();
	}
	AnimatorController  controller ;
	AnimatorStateMachine rootStateMachine ;
	AnimatorStateMachine stateMachineStand;

	void CreateAnimatorController(string _name)
	{
		// Creates the controller
		if(!Directory.Exists("Assets" + Path.DirectorySeparatorChar + "Mecanim"))
			Directory.CreateDirectory("Assets" + Path.DirectorySeparatorChar + "Mecanim");
		controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath ("Assets/Mecanim/"+_name+".controller");
		

		// Add parameters
		controller.AddParameter ("Animation_State", AnimatorControllerParameterType.Int);
		rootStateMachine = controller.layers[0].stateMachine;
		stateMachineStand = rootStateMachine.AddStateMachine("Animation_Controller"); 
		 
	}

	public AnimatorState CreateAnimationClip(string _filenames,string _path)
	{

		var animstate = stateMachineStand.AddState(_filenames); 
		AnimationClip _clip = (AnimationClip)AssetDatabase.LoadAssetAtPath("Assets"+_path +_filenames, typeof(AnimationClip));
		if (_clip != null)
		{
			SerializedObject serializedClip = new SerializedObject(_clip);
			AnimationClipSettings clipSettings = new AnimationClipSettings(serializedClip.FindProperty("m_AnimationClipSettings"));
			
			clipSettings.loopTime = true;
			
			serializedClip.ApplyModifiedProperties();
		}
		 
		animstate.motion = (Motion)_clip;
		if (animstate.motion == null)
			Debug.Log ("null Motion");
		return animstate;
	}

	void CreateAnimatorStateTransition(string _folderName)
	{
		List<AnimatorState> _tempstate = _animState [_folderName];
		if (_tempstate != null && _tempstate.Count > 0) {
			foreach (AnimatorState anim in _tempstate) {
				if (anim.name.Contains ("Default")) {
					for (int j=0; j<_tempstate.Count; j++) {
						if (_tempstate [j].name.Contains ("Default")||_tempstate[j].name.Contains ("default")) {
						//controller.layers[0].stateMachine.defaultState 	 = _animState[j];
						//	stateMachineStand.defaultState = _animState[j];
						} else {
						
							var animTransition = anim.AddTransition (_tempstate [j]);
							animTransition.AddCondition (UnityEditor.Animations.AnimatorConditionMode.Equals, (float)(j), "Animation_State");
							animTransition.duration = 0.025f;
							_animTransition.Add (animTransition);

						}
					}
				} else {
				 
					for (int i=0; i<_animState.Count; i++) {
						if (_tempstate [i].name.Contains ("Default")) {
							var animTransition = anim.AddTransition (_tempstate [i]);
							animTransition.hasExitTime = true;
							animTransition.duration = 0.025f;
							_animTransition.Add (animTransition);
						}
					}
				}
			}
			foreach (AnimatorState anim in _tempstate){
				if (!anim.name.Contains ("Default")) {
					for(int j=0;j<_tempstate.Count;j++){
						if(anim.name != _tempstate[j].name&&(!_tempstate[j].name.Contains("Default"))){
							var animTransition = anim.AddTransition (_tempstate [j]);
							animTransition.AddCondition (UnityEditor.Animations.AnimatorConditionMode.Equals, (float)(j), "Animation_State");
							animTransition.duration = 0.025f;
							_animTransition.Add (animTransition);
						}
					}
				}
				}
			}
		}

	GameObject _basemodel = null;
	void CreateModel(string _filenames,string _path,string _foldername)
	{	
		if (!_filenames.Contains ("@")) {
			_basemodel = (GameObject)AssetDatabase.LoadAssetAtPath ("Assets" + _path+ _filenames, typeof(GameObject));
			string _animatorcontrollername = _filenames.Replace(".fbx","").Trim();
			_basemodel.GetComponent<Animator> ().runtimeAnimatorController = (RuntimeAnimatorController)RuntimeAnimatorController.Instantiate (AssetDatabase.LoadAssetAtPath ("Assets/Mecanim/"+_animatorcontrollername+".controller", typeof(RuntimeAnimatorController)));
			GameObject _model = Instantiate (_basemodel);
			string _name = _filenames.Replace(".fbx","").Trim();
			_model.name = _name;

		}
		else 
		{
			_count +=1;
			if(!_filenames.Contains("Default"))
			CreateButton (_filenames,_count,_foldername);
		}


	}

	Dictionary<string, GameObject> _BTNPanel = new Dictionary<string,GameObject>();
	GameObject _canvasObj = null;
	public  void CreateCanvas()
	{
		if (GameObject.Find ("EventSystem") == null) {
			GameObject _eventSystem = (GameObject)AssetDatabase.LoadAssetAtPath ("Assets/Resources/Prefab/EventSystem.prefab", typeof(GameObject));
			_eventSystem.transform.name = _eventSystem.transform.name.Replace ("(clone)", "").Trim ();
			GameObject _EventSystem = Instantiate (_eventSystem);
			_EventSystem.name = _eventSystem.name;
		}
		GameObject _goButton = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/Resources/Prefab/Animation_Canvas.prefab", typeof(GameObject));
		_goButton.transform.name = _goButton.transform.name.Replace ("(clone)", "").Trim ();
		GameObject _Button = Instantiate (_goButton);
		_Button.name = _goButton.name; 
		if (_canvasObj == null) {
			_canvasObj = GameObject.Find(_goButton.name);
			if(_canvasObj.GetComponent<ChangeAnimation>() == null)
			_canvasObj.AddComponent<ChangeAnimation> ();
		}
	}


	public void CreateModelCanvas()
	{
		if (GameObject.Find("EventSystem") == null)
		{
			GameObject _eventSystem = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/Resources/Prefab/EventSystem.prefab", typeof(GameObject));
			_eventSystem.transform.name = _eventSystem.transform.name.Replace("(clone)", "").Trim();
			GameObject _EventSystem = Instantiate(_eventSystem);
			_EventSystem.name = _eventSystem.name;
		}
		GameObject _goButton = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/Resources/Prefab/Model_Canvas.prefab", typeof(GameObject));
		_goButton.transform.name = _goButton.transform.name.Replace("(clone)", "").Trim();
		GameObject _Button = Instantiate(_goButton);
		_Button.name = _goButton.name;
		if (_canvasObj == null)
		{
			_canvasObj = GameObject.Find(_goButton.name);
			if (_canvasObj.GetComponent<ChangeModels>() == null)
				_canvasObj.AddComponent<ChangeModels>();
			if (_modelNameList != null)
			{
				for (int i = 0; i < _modelNameList.Count; i++)
				{
					_canvasObj.GetComponent<ChangeModels>()._modelNames.Add(_modelNameList[i]);
				}
			}
			if (GameObject.Find("Main Camera") != null)
			{
				GameObject _Camera = GameObject.Find("Main Camera");
				_Camera.AddComponent<CameraRotate>();
			}
		}
	}

	void CreateButtonPanel(string _foldername)
	{
		GameObject _buttonPanel = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/Resources/Prefab/Button_Panel.prefab",typeof(GameObject));
		_buttonPanel.transform.name = _buttonPanel.transform.name.Replace ("(clone)", "").Trim ();
		GameObject _ButtonPanel = Instantiate (_buttonPanel);
		_ButtonPanel.name = _foldername+"_Panel";
		 
		if(_canvasObj != null){
		Transform _obj = _canvasObj.transform.Find ("Button_Panel").transform;
		_ButtonPanel.transform.SetParent (_obj); 
		if(_canvasObj.GetComponent<ChangeAnimation>() != null){
			if(_canvasObj.GetComponent<ChangeAnimation>()._basemodelname != null)
				_canvasObj.GetComponent<ChangeAnimation>()._basemodelname.Add(_foldername);
			else
			{
				_canvasObj.GetComponent<ChangeAnimation>()._basemodelname = new List<string>();
				if(_canvasObj.GetComponent<ChangeAnimation>()._basemodelname != null)
					_canvasObj.GetComponent<ChangeAnimation>()._basemodelname.Add(_foldername);
			}
			}
		}
		GameObject _OBJ = GameObject.Find (_foldername + "_Panel");
		_OBJ.GetComponent<RectTransform> ().localScale = Vector3.one;
		_OBJ.GetComponent<RectTransform> ().offsetMax = Vector3.zero;
		_OBJ.GetComponent<RectTransform> ().offsetMin = Vector3.zero;

		GameObject _tempOBJ = GameObject.Find (_foldername + "_Panel").transform.Find ("Viewport/Content").gameObject;
		if(_tempOBJ != null)
		_tempOBJ.name = _foldername + "_" +"Content";
		if (_BTNPanel != null) {
			if (!_BTNPanel.ContainsKey (_foldername))
				_BTNPanel.Add (_foldername, _tempOBJ); 
		}
		else
			_BTNPanel.Add (_foldername, _tempOBJ); 
	}


	void CreateButton(string _buttonname,int _val,string _foldername)
	{
		if (_BTNPanel != null) {
			GameObject _btn = new GameObject();
			_btn.AddComponent<Button> ();
			_btn.AddComponent<RectTransform>();
			_btn.AddComponent<Image>();
			_btn.GetComponent<Image>().sprite = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/Resources/Sprites/Button_Img.png",typeof(Sprite));
			GameObject _txt = new GameObject();
			_txt.AddComponent<Text>();
			if(_txt.GetComponent<RectTransform>() == null)
			_txt.AddComponent<RectTransform>();
			_buttonname = _buttonname.Replace(".fbx","").Trim();
			_txt.GetComponent<Text>().text = _buttonname;
			_txt.GetComponent<Text>().color = new Color(1,1,1,1);
			_txt.GetComponent<Text>().resizeTextForBestFit = true;
			_txt.GetComponent<Text>().alignment = TextAnchor.MiddleCenter; 
			_txt.GetComponent<Text>().resizeTextForBestFit = true;
			_txt.GetComponent<Text>().resizeTextMaxSize = 13;
			RectTransform  _txtrectTransform = _txt.GetComponent<RectTransform>();
			_txtrectTransform.SetParent(_btn.transform);
			_txtrectTransform.anchorMin = new Vector2(0, 0);
			_txtrectTransform.anchorMax = new Vector2(1, 1);
			_txtrectTransform.pivot = new Vector2(0.5f, 0.5f);
			RectTransform  _btnrectTransform = _btn.GetComponent<RectTransform>();
			if(_BTNPanel != null && _btnrectTransform != null && _BTNPanel.Count>0)
			_btnrectTransform.SetParent(_BTNPanel[_foldername].transform);
			_btnrectTransform.offsetMin = Vector2.zero;
			_btnrectTransform.offsetMax = Vector2.zero; 
			_btn.transform.localScale = new Vector3 (1, 1, 1);
			_btn.AddComponent<HorizontalLayoutGroup>();
			_btn.AddComponent<LayoutElement>();
			_btn.GetComponent<LayoutElement>().minHeight = 10;
			_btn.GetComponent<LayoutElement>().minWidth = 100;
			_btn.GetComponent<LayoutElement>().preferredHeight = 15;
			_btn.GetComponent<LayoutElement>().preferredWidth = 150;
			_btn.name = _buttonname; 	 

		}
	 
	}


	[MenuItem("Builds/WebglBuild")]
	public static void BuildWebGl() {
		string _sceneName = "TestScene.unity";
		string pathName = "Assets/"+_sceneName;
		EditorApplication.OpenScene (pathName);
		EditorApplication.SaveScene (pathName,true);
		string path = Application.dataPath;
		string _tempPath = null;
		if (path != null && path.Contains ("Assets"))
			_tempPath = path.Replace ("/Assets", "");
		path = _tempPath;
		List<string> levels = new List<string> ();
//		{
//			"Assets/Test_Levels/LoadingScreen.unity",
//			"Assets/Test_Levels/Main_Menu.unity",
//			"Assets/Test_Levels/Credits.unity",
//			"Assets/Test_Levels/Tutorial.unity",
//			"Assets/Test_Levels/Test_Level.unity",
//			"Assets/Test_Levels/Death_Menu.unity"
//		};

		if(pathName != null)
		levels.Add (pathName);

		string[] LevelPath = new string[levels.Count];
		if (levels != null && levels.Count > 0)
			for (int i=0; i<levels.Count; i++)
				LevelPath [i] = levels [i];
		UnityEditor.PlayerSettings.runInBackground = false;  
		BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
		buildPlayerOptions.scenes = LevelPath;
		buildPlayerOptions.locationPathName = path + "/" + "TestScene";
		buildPlayerOptions.target = BuildTarget.WebGL;
		buildPlayerOptions.options = BuildOptions.ShowBuiltPlayer;

		BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
		BuildSummary summary = report.summary;

		if (summary.result == BuildResult.Succeeded)
		{
			Debug.Log("WebGl build completed: " + summary.totalSize + " bytes");
		}

		if (summary.result == BuildResult.Failed)
		{
			Debug.Log("Error building Win64");
		}
	}


	
//Editing the Button Events through Unity Action Events 
//			_btn.GetComponent<Button>().onClick.AddListener(delegate {_btn.GetComponent<ChangeAnimation>().SelectedAnimation(_val,_foldername);}); 
	
//			Button tempButton = _btn.GetComponent<Button> ();	
//			 
//			tempButton.onClick.AddListener(delegate {ButtonClicked();});
	
//UnityEventTools.AddPersistentListener(tempButton.onClick, new UnityAction(() => _BTN.GetComponent<ChangeAnimation>().SelectedAnimation(_val)));


//	void CheckANimationstate()
//	{
//		for(int j=0;j<_animState.Count;j++)
//		{
//			Debug.Log(_animState[j].name);
//		}
//
////		for(int j=0;j<_animTransition.Count;j++)
////		{
////			Debug.Log(_animTransition[j].name);
////		}
//	}
			


}