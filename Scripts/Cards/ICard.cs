﻿namespace EESaga.Scripts.Cards;

using Godot;

public interface ICard
{
    public CardType CardType { get; set; }
    public string CardName { get; set; }
    public string CardDescription { get; set; }
    public int CardCost { get; set; }
    public CardTarget CardTarget { get; set; }
    public int CardRange { get; set; }
    public bool NeedTarget => CardTarget == CardTarget.Enemy || CardTarget == CardTarget.Ally;
}

public enum CardType
{
    Null,
    Attack,
    Defense,
    Special,
    Item,
}

public enum CardTarget
{
    Null,
    Self,
    Enemy,
    Ally,
    AllEnemies,
    AllAllies,
    All,
}

public class CardInfo(
    CardType cardType, string cardName, string cardDescription, int cardCost, CardTarget cardTarget, int cardRange = 1,
    int attackDamage = 0, int attackTimes = 0,
    int defenseValue = 0
    ) : ICard
{
    #region Card
    public CardType CardType { get; set; } = cardType;
    public string CardName { get; set; } = cardName;
    public string CardDescription { get; set; } = cardDescription;
    public int CardCost { get; set; } = cardCost;
    public CardTarget CardTarget { get; set; } = cardTarget;
    public int CardRange { get; set; } = cardRange;
    public bool NeedTarget => CardTarget == CardTarget.Enemy || CardTarget == CardTarget.Ally;
    #endregion

    #region Attack
    public int AttackDamage { get; set; } = attackDamage;
    public int AttackTimes { get; set; } = attackTimes;
    #endregion

    #region Defense
    public int DefenseValue { get; set; } = defenseValue;
    #endregion

    #region Special
    #endregion

    #region Item
    #endregion
}

public static class CardData
{
    public static CardInfo CAStrike = new(CardType.Attack, "C_A_STRIKE", "C_A_STRIKE_DESC", 1, CardTarget.Enemy, 1, attackDamage: 6, attackTimes: 1);
    public static CardInfo CDDefend = new(CardType.Defense, "C_D_DEFEND", "C_D_DEFEND_DESC", 1, CardTarget.Self, defenseValue: 3);
    public static CardInfo CSStruggle = new(CardType.Special, "C_S_STRUGGLE", "C_S_STRUGGLE_DESC", 1, CardTarget.Self);
    public static CardInfo CIECS = new(CardType.Item, "C_I_ECS", "C_I_ECS_DESC", 1, CardTarget.Self);
}