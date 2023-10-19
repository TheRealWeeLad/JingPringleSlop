using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public abstract class Enemy : MonoBehaviour
{
    public abstract string Name { get; protected set; }
    public abstract float MAXHEALTH { get; }
    public float Health { get; protected set; }
    public abstract float Damage { get; }
    public abstract float Speed { get; protected set; }
    public abstract uint MoneyReward { get; protected set; }
    public abstract Transform Target { get; protected set; }
    [field: SerializeField]
    public GameObject HealthPrefab { get; protected set; }
    [field: SerializeField]
    public GameObject StatusPrefab { get; protected set; }
    NavMeshAgent _agent;
    Material _material;
    GameObject healthBar;
    uint _numStatuses = 0;
    readonly Dictionary<StatusEffect, (RectTransform, ushort, IEnumerator)> _statuses = new();

    void Awake()
    {
        Health = MAXHEALTH;
        Target = GameObject.FindGameObjectWithTag("Player").transform;

        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = Speed;

        _material = GetComponent<MeshRenderer>().material;
    }
    void Start()
    {
        DisplayHealth();
    }
    void Update()
    {
        if (UIManagement.paused) return;
    }

    // Default Methods
    public bool IsDead() { return Health <= 0; }
    public IEnumerator Move()
    {
        _agent.isStopped = false;
        for (; ;)
        {
            Vector3 targetPos = new(Target.position.x, FPSController.LastGroundedY, Target.position.z);
            _agent.SetDestination(targetPos);
            yield return new WaitForSeconds(0.2f);
        }
    }
    public void StopMoving()
    {
        _agent.isStopped = true;
    }
    public void TakeDamage(float damage, float colorDelay)
    {
        Health -= damage;
        // Blink red
        if (_material.color.Equals(Color.black))
        {
            _material.color += Color.red * 0.2f;
            StartCoroutine(ChangeColorBack(colorDelay));
        }

        if (IsDead())
        {
            // Give Player Money
            PlayerStatManager.PlayerStats.Money += MoneyReward;
            PlayerStatUIManager.Instance.UpdateInfo();

            Destroy(gameObject);
            EnemyManager.RemoveEnemy(this);
        }
    }
    public void TakeDamage(float damage) { TakeDamage(damage, 0.2f); }
    IEnumerator ChangeColorBack(float delay)
    {
        yield return new WaitForSeconds(delay);
        _material.color -= Color.red * 0.2f;
    }
    void DisplayHealth()
    {
        healthBar = Instantiate(HealthPrefab, transform);
        healthBar.GetComponent<HealthBar>().SetAnchor(this);
    }

    // Update Status Effect Icon On Health Bar
    public void AddStatus(StatusEffect effect, ushort strength, IEnumerator coroutine)
    {
        (RectTransform, ushort, IEnumerator) tup;

        if (!_statuses.ContainsKey(effect))
        {
            Transform health = healthBar.transform;
            GameObject icon = Instantiate(StatusPrefab, health.position, health.rotation, health.GetChild(2));
            RectTransform iconTrans = icon.GetComponent<RectTransform>();
            iconTrans.anchoredPosition = new(0.03f * _numStatuses, 0);
            icon.GetComponent<Image>().sprite = StatusEffectManager.StatusIcons[effect];
            health.GetComponent<HealthBar>().AddIcon(icon.GetComponent<RectTransform>());

            tup = new()
            {
                Item1 = iconTrans,
                Item2 = strength,
                Item3 = coroutine
            };
            _numStatuses++;
        }
        else
        {
            tup = _statuses[effect];
            tup.Item2 += strength;
        }

        // Increment number on HUD
        TextMeshProUGUI numText = tup.Item1.GetChild(0).GetComponent<TextMeshProUGUI>();
        if (tup.Item2 > 1) numText.text = tup.Item2.ToString();

        _statuses[effect] = tup;
    }

    // Replace current status coroutine
    public void ReplaceCoroutine(StatusEffect effect, IEnumerator coroutine)
    {
        (RectTransform, ushort, IEnumerator) tup = _statuses[effect];
        StopCoroutine(tup.Item3);
        StartCoroutine(coroutine);
        tup.Item3 = coroutine;
        _statuses[effect] = tup;
    }

    // Check if enemy has Status already
    public bool HasStatus(StatusEffect effect) { return _statuses.ContainsKey(effect); }
    // Check enemy's status strength
    public ushort StatusStrength(StatusEffect effect)
    {
        if (HasStatus(effect)) return _statuses[effect].Item2;
        return 0;
    }

    // Abstract Methods
    protected abstract void Attack();
}