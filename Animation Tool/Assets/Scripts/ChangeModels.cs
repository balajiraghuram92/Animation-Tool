using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;


public class ChangeModels : MonoBehaviour
{
	public static ChangeModels _Instance;
	public List<string> _modelNames;
	public GameObject _Camera;

	CameraRotate _cameraScript;
	int _charecterCount;
	int _count;
	Transform _selectedCharector;
	Text _Charector_Name;
	Dictionary<string, GameObject> _models = new Dictionary<string, GameObject>(); 

	void Awake()
	{
		_Instance = this;
		_count = 0;
		_charecterCount = 0; 
		_Charector_Name = transform.Find("Panel/Charector_Name").GetComponent<Text>();
		if (GameObject.Find("Main Camera") != null)
		{
			GameObject _Camera = GameObject.Find("Main Camera");
			_cameraScript = _Camera.GetComponent<CameraRotate>();
			if(_cameraScript != null)
			{
				_cameraScript.targetOffset.y = -3;
				_cameraScript.distance = 30;
			}
		}
		foreach (string _modelName in _modelNames)
		{
			if (GameObject.Find(_modelName) != null)
				if (!_models.ContainsKey(_modelName))
					_models.Add(_modelName, GameObject.Find(_modelName));	
		}

		if (_modelNames.Count > 1)
		{
			foreach (string _model in _modelNames)
			{
				if (!_models[_model].name.Contains(_modelNames[0]))
					_models[_model].SetActive(false);
			}
			_Charector_Name.text = _modelNames[0];

		}
		ShowCharector(_modelNames[0], true);
	}

	 
	void ShowCharector(string _name, bool _val)
	{
		foreach (string _modelname in _modelNames)
		{
			if (_modelname.Contains(_name))
			{
				_selectedCharector = _models[_name].transform;
				_models[_name].SetActive(_val);
				_cameraScript.target = _models[_name].transform;
			}
			else
			{
				_models[_modelname].SetActive(false);
			}
		}
		
	}


	public void ShowPrevCharecter()
	{
		if (_charecterCount > 0 && _modelNames.Count > 1)
		{
			_charecterCount--;
			ShowCharector(_modelNames[_charecterCount], true);
			_Charector_Name.text = _modelNames[_charecterCount]; 
		}
	}

	public void ShowNextCharector()
	{
		if (_charecterCount < (_modelNames.Count - 1) && _modelNames.Count > 1)
		{
			_charecterCount++;
			ShowCharector(_modelNames[_charecterCount], true); 
			_Charector_Name.text = _modelNames[_charecterCount]; 
		}
	}

}
