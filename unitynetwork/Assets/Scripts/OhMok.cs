using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

struct MyPosition
{
    public float x;
    public float y;
    public int num;

}

public class OhMok : MonoBehaviour
{
    private enum GameProgress
    {
        None = 0,   // before Start
        Ready,      // Ready Game
        Turn,       // In Game
        Result,     // Game Result Win/Lose
        GameOver,   // Game Over
        Disconnect, // Disconnect
    };

    // Who's Turn
    private enum Turn
    {
        Own = 0,
        Opponent,
    };

    //Mark
    private enum Mark
    {
        White = 0,
        Black,
    };

    private enum Winner
    {
        None = 0,
        White,
        Black,
        Tie, // No winner
    };

    MyPosition myPosition;

    private const int rowNum = 20;  // size of board
    private const float waitTime = 1.0f;
    private const float turnTime = 10.0f;

    private int[,] spaces = new int[rowNum,rowNum];    // board size
    int xPos, yPos;
    int maxCnt = 0;

    private GameProgress progress;

    // whose Turn
    private Mark turn;

    // local sign
    private Mark localMark;

    // remote sign
    private Mark remoteMark;


    private float timer;
    private Winner winner;
    private bool isGameOver;
    private float currentTime;

    private int[,] curPos = new int[1,1];

    // Network
    private TransportTCP m_transport = null;

    private float step_count = 0.0f;



    public Sprite fieldTexture; // board
    public Sprite whiteTexture;
    public Sprite blackTexture;
    public Sprite youTexture;
    public Sprite winTexture;
    public Sprite loseTexture;

    public AudioSource audio;
    public AudioClip se_click;
    public AudioClip se_setMark;
    public AudioClip se_win;

    private static float SPACES_WIDTH = 535.0f;
    private static float SPACES_HEIGHT = 535.0f;

    private static float WINDOW_WIDTH = 535.0f;
    private static float WINDOW_HEIGHT = 535.0f;

    private GameObject bgm;



    private void Start()
    {
        GameObject obj = GameObject.Find("Network");
        bgm = GameObject.Find("BGM");

        m_transport = obj.GetComponent<TransportTCP>();

        if(m_transport != null)
        {
            m_transport.RegisterEventHandler(EventCallback);
        }

        Reset();
        isGameOver = false;
        timer = turnTime;
    }

    private void Update()
    {
        // check gameProgress
        switch(progress)
        {
            case GameProgress.Ready:
                UpdateReady();
                break;
            case GameProgress.Turn:
                UpdateTurn();
                break;
            case GameProgress.GameOver:
                UpdateGameOver();
                break;

        }
    }

    private void OnGUI()
    {
        switch(progress)
        {
            case GameProgress.Ready:
                DrawFieldAndMarks();
                break;
            case GameProgress.Turn:
                DrawFieldAndMarks();

                if(turn == localMark)
                {
                    DrawTime();
                }
                break;
            case GameProgress.Result:
                DrawFieldAndMarks();
                DrawWinner();
                {
                    GUISkin skin = GUI.skin;
                    GUIStyle style = new GUIStyle(GUI.skin.GetStyle("button"));
                    style.normal.textColor = Color.white;
                    style.fontSize = 30;

                    // position, name, type
                    if(GUI.Button(new Rect(Screen.width / 2 - 100, Screen.height / 2, 200,200),"끝",style))
                    {
                        progress = GameProgress.GameOver;
                        step_count = 0.0f;
                    }
                }
                break;

            case GameProgress.GameOver:
                DrawFieldAndMarks();
                DrawWinner();
                break;

            case GameProgress.Disconnect:
                DrawFieldAndMarks();
                NotifyDisconnection();
                break;
            default:
                break;
        }

    }



    void UpdateReady()
    {
        // wait start sign
        currentTime += Time.deltaTime;

        if(currentTime > waitTime)
        {
            // start Bgm
            bgm.GetComponent<AudioSource>().Play();

            // sign is done? start
            progress = GameProgress.Turn;
        }
    }

    void UpdateTurn()
    {
        bool setMark = false;

        if(turn == localMark)
        {
            setMark = DoOwnTurn();

            if(setMark == false && Input.GetMouseButton(0))
            {
                audio.clip = se_click;
                audio.Play();
            }
        }
        else
        {
            setMark = DoOppnentTurn();

            if(Input.GetMouseButtonDown(0))
            {
                audio.clip = se_click;
                audio.Play();
            }
        }

        if(setMark == false)
        {
            // check set pos
            return;
        }
        else
        {
            audio.clip = se_setMark;
            audio.Play();
        }

        winner = CheckInPlacingMarks();

        // if Winner is On
        if(winner != Winner.None)
        {
            if((winner == Winner.White && localMark == Mark.White) ||
                (winner == Winner.Black && localMark == Mark.Black))
            {
                audio.clip = se_win;
                audio.Play();
            }

            bgm.GetComponent<AudioSource>().Stop();

            progress = GameProgress.Result;
        }

        turn = (turn == Mark.White) ? Mark.Black : Mark.White;
        timer = turnTime;
        
    }

    void UpdateGameOver()
    {
        step_count += Time.deltaTime;

        if(step_count > 1.0f)
        {
            Reset();
            isGameOver = true;
        }


    }

    // My turn doing
    bool DoOwnTurn()
    {


        timer -= Time.deltaTime;
        // 랜덤한 곳에 바둑돌 배치
        if(timer <= 0.0f)
        {
            timer = 0.0f;
            do
            {
                xPos = Random.Range(0, rowNum);
                yPos = Random.Range(0, rowNum);
            } while (spaces[xPos,yPos] != -1);

            myPosition.x = xPos;
            myPosition.y = yPos;
        }
        // 플레이어가 놓은 곳에 바둑돌 배
        else
        {
            // check click
            bool isClicked = Input.GetMouseButtonDown(0);


            if(isClicked == false)
            {
                return false;
            }

            Vector3 pos = Input.mousePosition;
            Debug.Log(pos);

            myPosition = ConvertPositionToIndex(pos);

            // out of range;
            if(myPosition.num < 0)
            {
                return false;
            }

        }
        

        bool ret = SetMarkToSpace(myPosition, localMark);

        if(ret == false)
        {
            return false;
        }

        maxCnt++;

        // send info of ret
        byte[] buffer = new byte[3];
        buffer[0] = (byte)myPosition.x;
        buffer[1] = (byte)myPosition.y;
        buffer[2] = (byte)maxCnt;
        m_transport.Send(buffer, buffer.Length);

        return true;
    }

    bool DoOppnentTurn()
    {
        // get Info from other plater
        byte[] buffer = new byte[3];
        int recvSize = m_transport.Receive(ref buffer, buffer.Length);

        if(recvSize <= 0)
        {
            // nothing get
            return false;
        }

        myPosition.x = buffer[0];
        myPosition.y = buffer[1];
        maxCnt = buffer[2];


        bool ret = SetMarkToSpace(myPosition, remoteMark);

        if(ret == false)
        {
            return false;
        }


        return true;
    }


    MyPosition ConvertPositionToIndex(Vector3 pos)
    {
        MyPosition result;

        float sx = SPACES_WIDTH;
        float sy = SPACES_HEIGHT;
        float field = 512f;

        float left = ((float)Screen.width - sx) * 0.5f;
        float top = ((float)Screen.height - sy) * 0.5f;

        Debug.Log(top);

        float px = pos.x - left;
        float py = pos.y - top; 


        float divide = (float)rowNum;
        px = (int)(px * divide / field);
        py = (int)(py * divide / field);

        Debug.Log(px);
        Debug.Log(py);

        result.x = px;
        result.y = py;
        result.num = 0;

        if(px < 0.0f || px > field)
        {
            result.num = -1;
            // out of board
            return result;
        }

        if(py < 0.0f || py > field)
        {
            result.num = -1;

            return result;
        }

        return result;
    }

    bool SetMarkToSpace(MyPosition index, Mark mark)
    {
        if(spaces[(int)index.x, (int)index.y] == -1)
        {
            spaces[(int)index.x, (int)index.y] = (int)mark;
            return true;
        }

        // mark is on
        return false;
    }

    void DrawFieldAndMarks()
    {
        float sx = SPACES_WIDTH;
        float sy = SPACES_HEIGHT;
        float field = 512.0f;

        Rect rect = new Rect((Screen.width - WINDOW_WIDTH) * 0.5f,
                             (Screen.height - WINDOW_HEIGHT) * 0.5f,
                             WINDOW_WIDTH,
                             WINDOW_HEIGHT);
        Graphics.DrawTexture(rect, fieldTexture.texture);

        float left = ((float)Screen.width - sx) * 0.5f + 23f;
        float top = ((float)Screen.height - sy) * 0.5f + 23f;

        for (int i = 0; i < rowNum; i++)
        {
            for (int j = 0; j < rowNum; j++)
            {
                if(spaces[i,j] != -1)
                {
                    float divide = (float)rowNum;
                    float px = left + (i * field) / divide;
                    float py = top + ((rowNum - j) * field) / divide;

                    Texture texture = (spaces[i, j] == 0) ? whiteTexture.texture : blackTexture.texture;

                    float ofs = field / divide * 0.1f;
                    

                    Graphics.DrawTexture(new Rect(px - ofs, py + ofs, (field * 0.8f) / divide, (field * 0.8f) / divide), texture);
                }
            }
        }

        if(localMark == turn)
        {
            float offset = (localMark == Mark.White) ? -94.0f : sx + 36.0f;
            rect = new Rect(left + offset, top + 5.0f, 68.0f, 136.0f);
            Graphics.DrawTexture(rect, youTexture.texture);
        }

    }

    void DrawWinner()
    {
        float sx = SPACES_WIDTH;
        float sy = SPACES_HEIGHT;
        float left = ((float)Screen.width - sx) * 0.5f;
        float top = ((float)Screen.height - sy) * 0.5f;

        // 순서 텍스처 표시.
        float offset = (localMark == Mark.White) ? -94.0f : sx + 36.0f;
        Rect rect = new Rect(left + offset, top + 5.0f, 68.0f, 136.0f);
        Graphics.DrawTexture(rect, youTexture.texture);

        // 결과 표시.
        rect.y += 140.0f;

        if (localMark == Mark.White && winner == Winner.White ||
            localMark == Mark.Black && winner == Winner.Black)
        {
            Graphics.DrawTexture(rect, winTexture.texture);
        }

        if (localMark == Mark.White && winner == Winner.White ||
            localMark == Mark.Black && winner == Winner.Black)
        {
            Graphics.DrawTexture(rect, loseTexture.texture);
        }
    }

    void DrawTime()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 35;
        style.fontStyle = FontStyle.Bold;

        string str = "Time : " + timer.ToString("F3");

        style.normal.textColor = (timer > 5.0f) ? Color.black : Color.white;
        GUI.Label(new Rect(222, 5, 200, 100), str, style);

        style.normal.textColor = (timer > 5.0f) ? Color.white : Color.red;
        GUI.Label(new Rect(220, 3, 200, 100), str, style);
    }


    void NotifyDisconnection()
    {
        GUISkin skin = GUI.skin;
        GUIStyle style = new GUIStyle(GUI.skin.GetStyle("button"));
        style.normal.textColor = Color.white;
        style.fontSize = 25;

        float sx = 450;
        float sy = 200;
        float px = Screen.width / 2 - sx * 0.5f;
        float py = Screen.height / 2 - sy * 0.5f;

        string message = "회선이 끊겼습니다.\n\n버튼을 누르세요.";
        if (GUI.Button(new Rect(px, py, sx, sy), message, style))
        {
            // 게임이 종료됐습니다.
            Reset();
            isGameOver = true;
        }
    }

    int CheckCount(int Mark, int x, int y, int ox, int oy, int step)
    {
        if ((x < 0 || x > rowNum || y < 0 || y > rowNum) || Mark != spaces[x, y] || step == 4)
            return step;

        Debug.Log(step);
        return CheckCount(Mark, x + ox, y + oy, ox, oy, step + 1);

    }

    Winner CheckInPlacingMarks()
    {
        int curMark = spaces[(int)myPosition.x, (int)myPosition.y];

        

        // check col
        int ohmokCnt = 0;

        ohmokCnt += CheckCount(curMark, (int)myPosition.x, (int)myPosition.y + 1, 0, 1, 0);
        ohmokCnt += CheckCount(curMark, (int)myPosition.x, (int)myPosition.y - 1, 0, -1, 0);

        if(ohmokCnt == 4)
        {
            return curMark == 0 ? Winner.White : Winner.Black;
        }

        // check row
        ohmokCnt = 0;

        ohmokCnt += CheckCount(curMark, (int)myPosition.x + 1, (int)myPosition.y, 1, 0, 0);
        ohmokCnt += CheckCount(curMark, (int)myPosition.x - 1, (int)myPosition.y, -1, 0, 0);

        if (ohmokCnt == 4)
        {
            return curMark == 0 ? Winner.White : Winner.Black;
        }

        // check upRight
        ohmokCnt = 0;

        ohmokCnt += CheckCount(curMark, (int)myPosition.x + 1, (int)myPosition.y + 1, 1, 1, 0);
        ohmokCnt += CheckCount(curMark, (int)myPosition.x - 1, (int)myPosition.y - 1, -1, -1, 0);

        if (ohmokCnt == 4)
        {
            return curMark == 0 ? Winner.White : Winner.Black;
        }

        // check upLeft

        ohmokCnt = 0;

        ohmokCnt += CheckCount(curMark, (int)myPosition.x - 1, (int)myPosition.y + 1, -1, 1, 0);
        ohmokCnt += CheckCount(curMark, (int)myPosition.x + 1, (int)myPosition.y - 1, 1, -1, 0);

        if (ohmokCnt == 4)
        {
            return curMark == 0 ? Winner.White : Winner.Black;
        }


        if(maxCnt == rowNum*rowNum)
        {
            return Winner.Tie;
        }


        return Winner.None;
    }

    void Reset()
    {
        turn = Mark.White;
        progress = GameProgress.None;

        for (int i = 0; i < rowNum; i++)
        {
            for (int j = 0; j < rowNum; j++)
            {
                spaces[i, j] = -1;
            }
        }
    }

    public void GameStart()
    {
        progress = GameProgress.Ready;

        turn = Mark.White;

        if(m_transport.IsServer() == true)
        {
            localMark = Mark.White;
            remoteMark = Mark.Black;
        }
        else
        {
            localMark = Mark.Black;
            remoteMark = Mark.White;
        }

        isGameOver = false;
    }

    public bool IsGameOver()
    {
        return isGameOver;
    }

    public void EventCallback(NetEventState state)
    {
        switch(state.type)
        {
            case NetEventType.Disconnect:
                if (progress < GameProgress.Result && isGameOver == false)
                {
                    progress = GameProgress.Disconnect;
                }
                break;
        }
    }
}
