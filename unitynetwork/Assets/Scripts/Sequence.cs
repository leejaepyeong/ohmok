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

	[SerializeField]
	private GameObject game;

	[SerializeField]
	private GameObject TitlePanel;
	[SerializeField]
	private GameObject waitPanel;
	[SerializeField]
	private GameObject connectPanel;
	[SerializeField]
	private GameObject selectPanel;


	[SerializeField]
	private GameObject connetErrorTxt;

	public Sprite bgTexture;

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

			TitlePanel.SetActive(false);
			game.GetComponent<OhMok>().GameStart();
		}
	}

	void OnUpdateGame()
	{
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
				if(m_transport.IsConnected())
                {
					m_transport.Disconnect();
				}
				
				break;

			default:
				break;
		}

		TitlePanel.SetActive(true);
		waitPanel.SetActive(false);
		connectPanel.SetActive(false);
		selectPanel.SetActive(true);

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
		//DrawBg(true);

		serverAddress = GUI.TextField(new Rect(20,430,200,20),serverAddress);
	}

	/// <summary>
    /// 이미 만들어진 서버에 접속을 시도합니다
    /// </summary>
	public void ConnentBtn()
    {
		hostType = HostType.Client;
    }

	/// <summary>
    /// 게임을 만들어 접속자를 기다립니다.
    /// </summary>
	public void MakeServerBtn()
    {
		hostType = HostType.Server;
    }



	void OnGUICError()
	{
		connetErrorTxt.SetActive(true);
	}

	public void BackBtn()
    {
		connetErrorTxt.SetActive(false);
		m_mode = Mode.SelectHost;
		hostType = HostType.None;
    }

}
