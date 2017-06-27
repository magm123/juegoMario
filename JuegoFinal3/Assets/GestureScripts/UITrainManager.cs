using System;
using UnityEngine;

// manages the whole training interface
public class UITrainManager : MonoBehaviour 
{
	/// a reference
	GestureManager m_gestureManager;

	/// yet a reference
	SkeletonAvatarController m_avCtrl;
	
	/// 4 interfaces
	enum WorkingStage {Record, Play, Mark, Train};
	
	WorkingStage m_workStage = WorkingStage.Record;
	
	/// captions of the 4 buttons in the top left corner used for switching interfaces
	static string[] m_btnLabels = new string[]{"Observe/Record skel.", "Play skel.", "Mark key frames", "Train templates"};
	
	delegate void ButtonHandler();
	ButtonHandler[] m_handlers;
	
	JointData[] m_jointData;
	
	UIInfoObserver m_observer;
	
	UISkeletonRecorder m_recorder;
	
	UISkeletonPlayer m_player;
	
	UIKeyFrameMarker m_marker;
	
	UITemplateTrainer m_trainer;
	
	
	void Start () 
	{
		m_gestureManager = FindObjectOfType(typeof(GestureManager)) as GestureManager;
        if (m_gestureManager == null)
        {
            Debug.LogError("Please add an GestureManagerPrefab to the scene");
            return;
        }
		
		m_jointData = m_gestureManager.m_skReader.m_jointData;
		m_avCtrl = FindObjectOfType(typeof(SkeletonAvatarController)) as SkeletonAvatarController;
        if (m_avCtrl == null)
        {
            Debug.LogError("Please attach the script SkeletonAvatarController to an avatar and config the joints.");
            return;
        }
		m_avCtrl.SetJointDataSource(m_jointData); // m_gestureManager.m_skReader updates the JointData array, m_avCtrl uses them
		
		m_observer = new UIInfoObserver(m_gestureManager);
		m_gestureManager.m_gestureDetected += m_observer.OnNewGesture;
		
		m_recorder = new UISkeletonRecorder(m_jointData, m_gestureManager);
		
		m_player = new UISkeletonPlayer(m_recorder.m_skRecorder);
		
		m_marker = new UIKeyFrameMarker(m_recorder.m_skRecorder);
		
		m_trainer = new UITemplateTrainer(m_recorder.m_skRecorder, m_gestureManager.m_postFeature);
		
		m_handlers = new ButtonHandler[]{OnRecorderButton, OnPlayerButton, OnMarkerButton, OnTrainerButton};
		
		OnRecorderButton(); // the default interface
	}

	/// called once per frame
	void Update () 
	{
		if(m_workStage == WorkingStage.Record)
		{
			m_recorder.Update();
			m_observer.Update_realtime();
		}
		else if(m_workStage == WorkingStage.Play)
		{
			m_player.Update();
			if(m_player.m_isPlaying)
				m_observer.Update_realtime();
		}
		else if(m_workStage == WorkingStage.Mark)
		{
			m_marker.Update();
			string strData, strFrame;
			m_marker.GetObserverString(out strData, out strFrame);
			m_observer.Update_simple(strData, strFrame);
		}
		else if(m_workStage == WorkingStage.Train)
		{
			string strData, strEvent;
			m_trainer.GetObserverString(out strData, out strEvent);
			m_observer.Update_simple(strData, strEvent);
		}
	}
	
	/// draw the GUI
	void OnGUI ()
	{
		GUILayout.BeginArea(new Rect(0,0, Screen.width/3, Screen.height));
		GUILayout.BeginVertical();
		GUILayout.BeginHorizontal();
		
		foreach(WorkingStage ws in Enum.GetValues(typeof(WorkingStage)))
		{
			GUI.enabled = m_workStage != ws; // set the top left button for the current interface disabled
			if(GUILayout.Button(m_btnLabels[(int)ws]))
				m_handlers[(int)ws](); // if the button is clicked, execute the handler
		}

		GUI.enabled = true;
		GUILayout.EndHorizontal();
		
		GUILayout.Space(20);
		
		if(m_workStage == WorkingStage.Record)
			m_recorder.OnGUI();
		else if(m_workStage == WorkingStage.Mark)
			m_marker.OnGUI();
		else if(m_workStage == WorkingStage.Train)
			m_trainer.OnGUI();
		else if(m_workStage == WorkingStage.Play)
			m_player.OnGUI();
		
		GUILayout.Space(20);
		
		m_observer.OnGUI();
		GUILayout.EndVertical();
		GUILayout.EndArea();
	}
	
	void OnRecorderButton()
	{
		m_workStage = WorkingStage.Record;
		m_gestureManager.m_skReader.ResetSkeletonData();
		m_gestureManager.Pause = false;
		m_gestureManager.UseRealtimeData = true;
		m_avCtrl.SetDebugLineActive(true);
		m_observer.Reset();
		m_recorder.Init();
	}
	
	void OnPlayerButton()
	{
		m_workStage = WorkingStage.Play;
		m_gestureManager.m_skReader.ResetSkeletonData();
		m_avCtrl.ReturnToInitRot();
		m_gestureManager.Pause = false; // still detecting gestures
		m_gestureManager.UseRealtimeData = false; // but do not use real time data
		m_avCtrl.SetDebugLineActive(false);
		m_player.Init();
		m_observer.Reset();
	}
	
	void OnMarkerButton()
	{
		m_workStage = WorkingStage.Mark;
		m_gestureManager.m_skReader.ResetSkeletonData();
		m_avCtrl.ReturnToInitRot();
//		m_avCtrl.Pause = true;
		m_gestureManager.Pause = true;
		m_avCtrl.SetDebugLineActive(false);
		m_marker.Init();
	}
	
	void OnTrainerButton()
	{
		m_workStage = WorkingStage.Train;
		m_gestureManager.Pause = true;
		m_avCtrl.SetDebugLineActive(false);
		m_trainer.Init();
	}
	
}
