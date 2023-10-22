using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static readonly List<Enemy> EnemyList = new();
    static readonly List<IEnumerator> enemyMoves = new();

    [Header("Enemy Prefabs")]
    public GameObject GlimboPrefab;

    static readonly List<GameObject> _prefabList = new();

    // DEBUG
    const float SPAWNCD = 1;
    static float _timeUntilSpawn = 0;

    private void Awake()
    {
        _prefabList.Add(GlimboPrefab);
        EnemyList.Clear();
    }

    private void Update()
    {
        if (_timeUntilSpawn > 0) _timeUntilSpawn -= Time.deltaTime;
    }

    public static void StopEnemyMovement()
    {
        for (int i = 0; i < EnemyList.Count; i++)
        {
            Enemy enemy = EnemyList[i];
            enemy.StopCoroutine(enemyMoves[i]);
            enemy.StopMoving();
        }
    }
    public static void StartEnemyMovement()
    {
        for (int i = 0; i < EnemyList.Count; i++)
        {
            Enemy enemy = EnemyList[i];
            IEnumerator newMove = enemy.Move();
            enemy.StartCoroutine(newMove);
            enemyMoves[i] = newMove;
        }
    }

    public static void AddEnemy(Enemy enemy)
    {
        EnemyList.Add(enemy);
        IEnumerator move = enemy.Move();
        enemyMoves.Add(move);
        enemy.StartCoroutine(move);
    }

    public static void RemoveEnemy(Enemy enemy)
    {
        int idx = EnemyList.IndexOf(enemy);
        EnemyList.RemoveAt(idx);
        enemyMoves.RemoveAt(idx);
    }

    public static void Spawn(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        GameObject pref = Instantiate(prefab, pos, rot);
        Enemy enemy = pref.GetComponent<Enemy>();
        AddEnemy(enemy);
    }

    public static void SpawnRandom(Vector3 pos, Quaternion rot)
    {
        if (_timeUntilSpawn <= 0)
        {
            int idx = (int)(Random.value * _prefabList.Count);
            Enemy enemy = Instantiate(_prefabList[idx], pos, rot).GetComponent<Enemy>();
            _timeUntilSpawn = SPAWNCD;
            AddEnemy(enemy);
        }
    }
}
