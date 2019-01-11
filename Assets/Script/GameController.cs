using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using UniRx;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class GameController : MonoBehaviour
{
    public const int matching = 3;
    public const int matchX = 4;
    public const int matchR = 5;
    private const int clearsore = 5000;
    /*
    private enum GameState
    {
        Idle,
        PieceMove,
        MatchCheck,
        DeletePiece,
        FillPiece,
    }*/

    public enum FlickDirection
    {
        up,
        right,
        down,
        left,
        touch,
    }

    [SerializeField] private GameObject stage;
    private Board board;
    //private GameState currentState;
    private Piece selectedPiece;
    private Vector3 start;
    private Vector3 end;
    private bool flag = false;
    public static ReactiveProperty<int> score = new ReactiveProperty<int>(0);
    [SerializeField] private Text score_text;
    [SerializeField] private Text clear;
    public static float rate;

    // Use this for initialization
    void Start()
    {
        DOTween.SetTweensCapacity(500, 50);
        board = stage.GetComponent<Board>();
        board.InitBoard();
        //currentState = GameState.Idle;
        score.Value = 0;

        Observable.EveryUpdate().Where(_ => Input.GetMouseButtonDown(0)).Where(_ => flag == false).Subscribe(_ => {
            StartCoroutine("Cycle");
        }).AddTo(this);
        score.Subscribe(x => score_text.text = x.ToString()).AddTo(this);
        score.Where(_ => score.Value > clearsore).Subscribe(_ =>
        {
            StartCoroutine("Restart");
        }).AddTo(this);

    }
    /*
    // Update is called once per frame
    void Update () {
        switch(currentState){
            case GameState.Idle:
                if(score>=5000&&flag==false){
                    clear.gameObject.SetActive(true);
                    Observable.Timer(TimeSpan.FromSeconds(2f)).Subscribe(_ => OnClickreturn());
                    flag = true;
                }
                else if(score<5000){
                    Idle();
                    rate = 0.8f;
                }
                break;
            case GameState.PieceMove:
                PieceMove();
                break;
            case GameState.MatchCheck:
                if (flag == false)
                {
                    flag = true;
                    Observable.Timer(TimeSpan.FromSeconds(0.2f)).Subscribe(_ => MatchCheck());
                }
                //MatchCheck();
                break;
            case GameState.DeletePiece:
                DeletePiece();
                rate += 0.2f;
                break;
            case GameState.FillPiece:
                if (flag == false)
                {
                    flag = true;
                    Observable.Timer(TimeSpan.FromSeconds(0.5f)).Subscribe(_ => FillPiece());
                }
                //FillPiece();
                break;
            default:
                break;
        }
        score_text.text = score.ToString();
    }

    void Idle(){
        if(Input.GetMouseButtonDown(0)){
            selectedPiece = board.GetNearestPiece(MouseWorldPos());
            start = MouseWorldPos();
            currentState = GameState.PieceMove;
        rate = 0.8f;
        }
    }
    void PieceMove(){
        if(Input.GetMouseButton(0)){
            var piece = board.GetNearestPiece(MouseWorldPos());
            if(piece!=selectedPiece){
                board.SwitchPiece(selectedPiece, piece);
            }
        }else if(Input.GetMouseButtonUp(0)){
            currentState = GameState.MatchCheck;
        }
        if(Input.GetMouseButtonUp(0)){
            end = MouseWorldPos();
            board.SwitchPiece(board.SelectNext(selectedPiece, Flick()), selectedPiece);
            currentState = GameState.MatchCheck;
        }

    }
    void MatchCheck(){
        if(board.HasMatch()){
            currentState = GameState.DeletePiece;
        }else{
            currentState = GameState.Idle;
        }
        flag = false;
    }
    void DeletePiece(){
        board.DeleteMatchPiece();
        currentState = GameState.FillPiece;
    }
    void FillPiece(){
        board.FillPiece();
        currentState = GameState.MatchCheck;
        flag = false;
    }*/

    void Idle()
    {
        selectedPiece = board.GetNearestPiece(MouseWorldPos());
        start = MouseWorldPos();
        rate = 0.8f;
    }
    void PieceMove()
    {
        end = MouseWorldPos();
        if (Flick() != FlickDirection.touch)
            board.SwitchPiece(board.SelectNext(selectedPiece, Flick()), selectedPiece);

    }
    void DeletePiece()
    {
        board.DeleteMatchPiece();
    }
    void FillPiece()
    {
        board.FillPiece();
    }

    Vector3 MouseWorldPos()
    {
        Vector3 screenpos = Input.mousePosition;
        return Camera.main.ScreenToWorldPoint(screenpos);
    }

    FlickDirection Flick()
    {
        float dirX = end.x - start.x;
        float dirY = end.y - start.y;
        if (Mathf.Abs(dirY) < Mathf.Abs(dirX))
        {
            if (dirX > 0.1f)
            {
                return FlickDirection.right;
            }
            else if (dirX < -0.1f)
            {
                return FlickDirection.left;
            }
        }
        if (Mathf.Abs(dirY) > Mathf.Abs(dirX))
        {
            if (dirY > 0.1f)
            {
                return FlickDirection.up;
            }
            else if (dirY < -0.1f)
            {
                return FlickDirection.down;
            }
        }
        return FlickDirection.touch;
    }

    public void OnClickreturn()
    {
        SceneManager.LoadScene("start");
    }

    private IEnumerator Cycle()
    {
        flag = true;
        Idle();
        yield return Observable.EveryUpdate().FirstOrDefault(_ => Input.GetMouseButtonUp(0)).ToYieldInstruction();
        PieceMove();
        while (board.HasMatch() || board.HasBomb() || board.Rmove)
        {
            yield return new WaitForSeconds(0.2f);
            DeletePiece();
            rate += 0.2f;
            yield return new WaitForSeconds(0.5f);
            FillPiece();
            yield return new WaitForSeconds(1f);
        }
        flag = false;
    }

    private IEnumerator Restart()
    {
        clear.gameObject.SetActive(true);
        yield return new WaitForSeconds(3f);
        SceneManager.LoadScene("start");
    }
}
