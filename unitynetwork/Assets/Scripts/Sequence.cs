using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using UnityEngine.UI;

public class Sequence : MonoBehaviour
{
	private Mode m_mode;

	private string serverAddress;

	private HostType hostType;

	private const int m_port = 50765;

	private TransportTCP m_transport = null;

	private int m_counter = 0;

	public Sprite bgTexture;
	public Sprite pushTexture;

	private static float WINDOW_WIDTH = 640.0f;
	private static float WINDOW_HEIGHT = 480.0f;

	enum Mode
	{
		SelectHost = 0,
		Connection,
		Game,
		Disconnection,
		Error,
	};

	enum HostType
	{
		None = 0,
		Server,
		Client,
	};


	void Awake()
	{
		m_mode = Mode.SelectHost;
		hostType = HostType.None;
		serverAddress = "";

		// Network 클래스의 컴포넌트 취득.
		GameObject obj = new GameObject("Network");
		m_transport = obj.AddComponent<TransportTCP>();
		DontDestroyOnLoad(obj);

		// 호스트명을 가져옵니다.
		string hostname = Dns.GetHostName();
		// 호스트명에서 IP주소를 가져옵니다.
		IPAddress[] adrList = Dns.GetHostAddresses(hostname);
		serverAddress = adrList[0].ToString();
	}

	void Update()
	{

		switch (m_mode)
		{
			case Mode.SelectHost:
				OnUpdateSelectHost();
				break;

			case Mode.Connection:
				OnUpdateConnection();
				break;

			case Mode.Game:
				OnUpdateGame();
				break;

			case Mode.Disconnection:
				OnUpdateDisconnection();
				break;
		}

		++m_counter;
	}

	//
	void OnGUI()
	{
		switch (m_mode)
		{
			case Mode.SelectHost:
				OnGUISelectHost();
				break;

			case Mode.Connection:
				OnGUIConnection();
				break;

			case Mode.Game:
				break;

			case Mode.Disconnection:
				break;

			case Mode.Error:
				OnGUICError();
				break;
		}
	}


	// Sever 또는 Client 선택화면
	void OnUpdateSelectHost()
	{

		switch (hostType)
		{
			case HostType.Server:
				{
					bool ret = m_transport.StartServer(m_port, 1);
					m_mode = ret ? Mode.Connection : Mode.Error;
				}
				break;

			case HostType.Client:
				{
					bool ret = m_transport.Connect(serverAddress, m_port);
					m_mode = ret ? Mode.Connection : Mode.Error;
				}
				break;

			default:
				break;
		}
	}

	void OnUpdateConnection()
	{
		if (m_transport.IsConnected() == true)
		{
			m_mode = Mode.Game;

			GameObject game = GameObject.Find("Ohmok");
			game.GetComponent<OhMok>().GameStart();
		}
	}

	void OnUpdateGame()
	{
		GameObject game = GameObject.Find("Ohmok");

		if (game.GetComponent<OhMok>().IsGameOver() == true)
		{
			m_mode = Mode.Disconnection;
		}
	}


	void OnUpdateDisconnection()
	{
		switch (hostType)
		{
			case HostType.Server:
				m_transport.StopServer();
				break;

			case HostType.Client:
				m_transport.Disconnect();
				break;

			default:
				break;
		}

		m_mode = Mode.SelectHost;
		hostType = HostType.None;
		//serverAddress = "";
		// 호스트명을 가져옵니다.
		string hostname = Dns.GetHostName();
		// 호스트명에서 IP 주소를 가져옵니다.
		IPAddress[] adrList = Dns.GetHostAddresses(hostname);
		serverAddress = adrList[0].ToString();
	}


	void OnGUISelectHost()
	{
		// 배경 표시.
		DrawBg(true);

		if (GUI.Button(new Rect(20, 290, 150, 20), "대전 상대를 기다립니다"))
		{
			hostType = HostType.Server;
		}

		// 클라이언트를 선택했을 때 접속할 서버 주소를 입력합니다.
		if (GUI.Button(new Rect(20, 380, 150, 20), "대전 상대와 접속합니다"))
		{
			hostType = HostType.Client;
		}

		Rect labelRect = new Rect(20, 410, 200, 30);
		GUIStyle style = new GUIStyle();
		style.fontStyle = FontStyle.Bold;
		style.normal.textColor = Color.white;
		GUI.Label(labelRect, "상대방 IP 주소", style);
		labelRect.y -= 2;
		style.fontStyle = FontStyle.Normal;
		style.normal.textColor = Color.black;
		GUI.Label(labelRect, "상대방 IP 주소", style);

		serverAddress = GUI.TextField(new Rect(20, 430, 200, 20), serverAddress);
	}


	void OnGUIConnection()
	{
		// 배경 표시.
		DrawBg(false);

		// 클라이언트를 선택했을 때 접속할 서버 주소를 입력합니다.
		GUI.Button(new Rect(84, 335, 160, 20), "대전 상대를 기다립니다");
	}

	void OnGUICError()
	{
		// 배경 표시.
		DrawBg(false);

		float px = Screen.width * 0.5f - 150.0f;
		float py = Screen.height * 0.5f;

		if (GUI.Button(new Rect(px, py, 300, 80), "접속할 수 없습니다.\n\n버튼을 누르세요"))
		{
			m_mode = Mode.SelectHost;
			hostType = HostType.None;
		}
	}

	// 배경 표시.
	void DrawBg(bool blink)
	{
		// 배경을 그립니다.
		Rect bgRect = new Rect(Screen.width / 2 - WINDOW_WIDTH * 0.5f,
							 Screen.height / 2 - WINDOW_HEIGHT * 0.5f,
							 WINDOW_WIDTH,
							 WINDOW_HEIGHT);
		Graphics.DrawTexture(bgRect, bgTexture.texture);

		// 버튼을 누르세요.
		if (blink && m_counter % 120 > 20)
		{
			Rect pushRect = new Rect(84, 335, 220, 25);
			Graphics.DrawTexture(pushRect, pushTexture.texture);
		}
	}
}
