using ExitGames.Client.Photon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;
using Random = System.Random;

public class Game : MonoBehaviour
{
     [SerializeField] private List<SOCard> cardsList = new List<SOCard>();

     [SerializeField] private static Card _cardPrefab;
     private static Transform _otherPlayerCardsUiParent, _mainPlayerCardsUiParent, _deckCardsUiParent;

    private static List<GameObject> PlayerCards = new List<GameObject>(), ActualPlayerCards = new List<GameObject>();
    public static List<GameObject> DeckCards = new();
    public static List<SOCard> cards = new();

    public TMPro.TMP_Text debug;

    private bool isFinish = false;

    public static PhotonPlayer[] players;

    public static PhotonPlayer master;

    public static PhotonView pv;

    private void Awake()
    {
        for (int i = 0; i < CardManager._cards.Count; i++)
        {
            cards.Add(CardManager._cards[i]);
        }
    }

    private void Start()
    {
        _mainPlayerCardsUiParent = GameObject.Find("Cards MainPlayer").transform;
        _otherPlayerCardsUiParent = GameObject.Find("Cards OtherPlayers").transform;
        _deckCardsUiParent = GameObject.Find("Cards Deck").transform;
        GameObject go = (GameObject)Resources.Load("Card");
        _cardPrefab = go.GetComponent<Card>();
        var tempGameManagerInstance = GameManager.instance;
        for (int i = 0; i < GameManager.instance.piles.Length; i++)
        {
            GameManager.instance.piles[i] = new Pile();
        }

        players = PhotonNetwork.playerList;
    }

    public static void Game1()
    {
        DisplayCardsLocal();
        DisplayPiles();
        foreach (PhotonPlayer pla in players)
        {
            if (pla.IsMasterClient)
            {
                master = pla;
            }
        }
        if (PhotonNetwork.isMasterClient)
        {
            Debug.Log("Game is on !");

            PhotonNetwork.RPC(pv, "DisplayCards", PhotonTargets.All,false,(int[])players[0].CustomProperties["Deck"]);
        }
    }


    public static void DisplayCards(int[] ids)
    {
        foreach (GameObject go in ActualPlayerCards) { Destroy(go); }
        ActualPlayerCards.Clear();
        for (int j = 0; j < ids.Length; j++)
        {
            Card card;
            card = Instantiate(_cardPrefab, _otherPlayerCardsUiParent);
            ActualPlayerCards.Add(card.gameObject);            
            card.card = CardManager.GetCard(ids[j]);
            card.LoadImage();
        }
    }
    public static void DisplayCardsLocal()
    {
        int[] ids = (int[])PhotonNetwork.player.CustomProperties["Deck"];
        foreach (GameObject go in PlayerCards) { Destroy(go); }
        PlayerCards.Clear();
        for (int j = 0; j < ids.Length; j++)
        {
            Card card;
            card = Instantiate(_cardPrefab, _mainPlayerCardsUiParent);
            PlayerCards.Add(card.gameObject);
            card.card = CardManager.GetCard(ids[j]);
            card.LoadImage();
        }
    }

    public static void DisplayPiles()
    {
        foreach (GameObject go in DeckCards) { Destroy(go); }
        DeckCards.Clear();
        for (int j = 0; j < cards.Count; j++)
        {
            Card card;
            card = Instantiate(_cardPrefab, _deckCardsUiParent);
            DeckCards.Add(card.gameObject);
            card.card = cards[j];
            card.LoadImage();
        }
    }


    private static void TurnInitialization(int i)
    {
        // i player turn
        //GameManager.instance.currentPlayer = GameManager.instance.players[i];
        PhotonNetwork.RPC(pv, "DisplayCards", PhotonTargets.All, false, (int[])players[i].CustomProperties["Deck"]);
    }

    private static void CardEffectOnOtherPlayers()
    {
        SOCard playerCard;
        //Player[] playersArray = GameManager.instance.players;

        Player currentPlayer = GameManager.instance.currentPlayer;

        for (int m = 0; m < players.Length; m++)
        {
            if (players[m].NickName == players[GameManager.instance.turn].NickName)
            {
                Debug.Log("Si �crit c'est bon");
                continue;
            }

            for (int j = 0; j < ((int[])players[m].CustomProperties["Deck"]).Length; j++)
            {
                playerCard = CardManager.GetCard(((int[])players[m].CustomProperties["Deck"])[j]);

                foreach (int act in playerCard.activation)
                {

                    if (playerCard.color == SOCard.EColor.Rouge && act == Die.face)
                    {
                        //currentPlayer.money -= playerCard.effect;
                        PhotonNetwork.RPC(pv, "AddGoldRPC", players[GameManager.instance.turn], true, -1);
                        //playersArray[m].money += playerCard.effect;
                        PhotonNetwork.RPC(pv, "AddGoldRPC", players[m], true, 1);
                        Debug.Log($"{players[m].NickName} Get coins --> Red color now {PlayerNetwork.GetGold()} gold");
                        Debug.Log($"{players[GameManager.instance.turn].NickName} Loose coins --> Red color now {players[GameManager.instance.turn].CustomProperties["Gold"]} gold");
                    }
                }
            }
        }
    }
    

    private static void NextTurn()
    {
        GameManager.instance.turn++;

        if (GameManager.instance.turn >= players.Length)
        {
            GameManager.instance.turn = 0;
        }
    }

    public static void Play()
    {
        if (PhotonNetwork.player.IsMasterClient)
        {
            TurnInitialization(GameManager.instance.turn);
            CardEffectOnOtherPlayers();
            PhotonNetwork.RPC(pv, "CardEffetOnPlayer", PhotonTargets.All, true, players[GameManager.instance.turn].NickName, Die.face);
            NextTurn();
            PhotonNetwork.RPC(pv, "DisplayCards", PhotonTargets.All, false, (int[])players[GameManager.instance.turn].CustomProperties["Deck"]);
        }
    }

    public static PhotonPlayer GetPlayerByName(string name)
    {
        foreach(PhotonPlayer player in players)
        {
            if (player.NickName == name)
                return player;
        }
        return null;
    }

    public void OnMasterClientSwitched(PhotonPlayer newMasterClient)
    {
        master = newMasterClient;
    }

    public static int GetCardNumberOfType(string cardName)
    {
        return (int)master.CustomProperties[cardName];
    }
    public static void TakeCard(string cardName)
    {
        Hashtable hash = new Hashtable();
        hash.Add(cardName, GetCardNumberOfType(cardName)-1);
        master.SetCustomProperties(hash);
    }
    public static void RemoveMiddleCard(string name)
    {
        PhotonNetwork.RPC(pv, "RemoveCentercCard", PhotonTargets.Others, true, name);
    }
}
