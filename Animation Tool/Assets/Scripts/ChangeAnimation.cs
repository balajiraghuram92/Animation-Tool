using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;


public class ChangeAnimation : MonoBehaviour {


	public List<string> _basemodelname ; 
	public GameObject _Camera;
	public List<GameObject> ButtonsOBJ;
	
	Dictionary<string,GameObject> _BaseModel =  new Dictionary<string,GameObject>() ;
	Dictionary<string,Animator> _baseAnimator = new Dictionary<string,Animator>();
	Dictionary<string,Button[]> _animationsButton = new Dictionary<string,Button[]>() ;
	int _count;

	Text _Charector_Name;
	Button[] _tempbutton;
	Button[] _CharectorBtns;
	int _charecterCount;
	// Use this for initialization
	void Awake () {

		
		SetCamera ();

		foreach (string _modelName in _basemodelname) {

			if (GameObject.Find (_modelName+"_"+"Content") != null)
				if(ButtonsOBJ != null)
				ButtonsOBJ.Add(GameObject.Find (_modelName+"_"+"Panel"));
				if(!_animationsButton.ContainsKey(_modelName))
					_animationsButton.Add(_modelName,GameObject.Find (_modelName+"_"+"Content").GetComponentsInChildren<Button> ()); 

			if (GameObject.Find (_modelName) != null)
				if(!_BaseModel.ContainsKey(_modelName))
					_BaseModel.Add(_modelName,GameObject.Find (_modelName));

			if (_BaseModel != null)
				if(!_baseAnimator.ContainsKey(_modelName))
					_baseAnimator.Add(_modelName,_BaseModel[_modelName].GetComponent<Animator> ());

			if (_animationsButton != null){
				_tempbutton = _animationsButton[_modelName];
			foreach (Button btn in _tempbutton) {
					_count += 1;
					AddEventToButton (btn, _count);
				}
			_count = 0;
			}
		}

		 if (_basemodelname.Count > 1) {
			if (_CharectorBtns != null)
				_CharectorBtns [0].interactable = false;
			if (_BaseModel != null && _basemodelname.Count > 1)
				if(ButtonsOBJ != null&& ButtonsOBJ.Count>1)
					foreach (GameObject _obj in ButtonsOBJ)
						{
							if(!_obj.name.Contains(_basemodelname[0]))
								_obj.SetActive (false);
						}
					foreach (string _model in _basemodelname)
						{	 
							if(!_BaseModel [_model].name.Contains(_basemodelname[0]))
								_BaseModel [_model].SetActive (false);
						}
					_Charector_Name.text = _basemodelname[0];
				 
		} else {
			if(_CharectorBtns.Length>1){
			foreach(Button btn in _CharectorBtns)
				btn.interactable = false;
			}

		}
 }

	void SetButtons(string _name)
	{
		foreach (GameObject _obj in ButtonsOBJ)
		{
			if(!_obj.name.Contains(_name))
				_obj.SetActive (false);
			else
				_obj.SetActive (true);
		}
	}

	void ShowCharector(string _name,bool _val)
	{
		foreach(string _modelname  in _basemodelname)
		{
			if(_modelname.Contains(_name))
				_BaseModel[_name].SetActive(_val);
			else
				_BaseModel[_modelname].SetActive(false);
		}
	}

	void AddEventToButton(Button _btn,int _val)
	{
		 _btn.GetComponent<Button>().onClick.AddListener(delegate {SelectedAnimation(_val,_btn.name);});
	}
	
	void SetCamera()
	{

		
		_count = 0;
		_charecterCount = 0;
		_tempbutton = null;
		_CharectorBtns = null;
		ButtonsOBJ = new List<GameObject> ();

		_CharectorBtns = GameObject.Find ("Charector_Panel").GetComponentsInChildren<Button> ();
		_Charector_Name = transform.Find ("Charector_Name").GetComponent<Text> ();

		if(GameObject.Find ("Main Camera") !=  null)
			_Camera = GameObject.Find ("Main Camera");
			
		if(_Camera != null)
		{
			GameObject _tempCamera = (GameObject)Resources.Load("Prefab/Camera");;
			_Camera.transform.position = _tempCamera.transform.position;
			_Camera.transform.rotation = _tempCamera.transform.rotation;
			_Camera.transform.localScale = _tempCamera.transform.localScale;
		}

		if (_CharectorBtns != null) {
			_CharectorBtns[0].GetComponent<Button> ().onClick.AddListener (delegate {
				ShowPrevCharecter();
			});
			_CharectorBtns[1].GetComponent<Button> ().onClick.AddListener (delegate {
				ShowNextCharector ();
			});
		}
		 

	}

	void CharectorChangeBTN()
	{
		if (_charecterCount == 0 && _CharectorBtns.Length > 0) {
			_CharectorBtns [0].GetComponent<Button> ().interactable = false;
			_CharectorBtns[1].GetComponent<Button> ().interactable = true;
		}
		if ((_charecterCount == (_basemodelname.Count-1)) && _CharectorBtns.Length > 0){
			_CharectorBtns[1].GetComponent<Button> ().interactable = false;
			_CharectorBtns[0].GetComponent<Button> ().interactable = true;
		}
	}
	void ShowPrevCharecter()
	{
		if (_charecterCount > 0 && _basemodelname.Count>1) {
			_charecterCount--;
			ShowCharector (_basemodelname [_charecterCount], true);
			SetButtons(_basemodelname [_charecterCount]);
			_Charector_Name.text = _basemodelname [_charecterCount];
			CharectorChangeBTN();
		}
	}

	void ShowNextCharector()
	{
		if (_charecterCount < (_basemodelname.Count-1) &&_basemodelname.Count>1) {
			_charecterCount++;
			ShowCharector (_basemodelname [_charecterCount], true);
			SetButtons(_basemodelname [_charecterCount]);
			_Charector_Name.text = _basemodelname [_charecterCount];
			CharectorChangeBTN();
		}
	}
	 

	public  void SelectedAnimation(int _val,string _name)
	{
		Animator _tempAnimator = null;
		string[] _tmpname = _name.Split (new string[]{"@"}, System.StringSplitOptions.None);
		if (_baseAnimator != null) 
		{
			foreach(string _bsname in _basemodelname)
			{
				if(_bsname.Contains(_tmpname[0])){
					_tempAnimator = _baseAnimator[_bsname];
					if(_tempAnimator != null)
					_tempAnimator.SetInteger ("Animation_State", _val);
				}
				 
			}

		}

		 
	}


	 


}
