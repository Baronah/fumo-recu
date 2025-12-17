using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Candleknight : EnemyBase
{
    [SerializeField] private float checkInterval = 0.1f;

    Tilemap shroudedZonesTiles;

    private Dictionary<Vector3Int, TileBase> hiddenTiles = new Dictionary<Vector3Int, TileBase>();
    private HashSet<Vector3Int> currentlyHiddenPositions = new HashSet<Vector3Int>();

    public override void InitializeComponents()
    {
        base.InitializeComponents();
        var darkzones = FindFirstObjectByType<DarkTile>();
        if (darkzones)
        {
            shroudedZonesTiles = darkzones.GetComponent<Tilemap>();
            StartCoroutine(RemoveDarkZones());
        }
    }

    IEnumerator RemoveDarkZones()
    {
        while (true)
        {
            Vector3Int centerCell = shroudedZonesTiles.WorldToCell(transform.position);
            HashSet<Vector3Int> newHiddenPositions = new HashSet<Vector3Int>();

            // Calculate which tiles should be hidden
            int radiusInt = Mathf.CeilToInt(attackRange);
            for (int x = -radiusInt; x <= radiusInt; x++)
            {
                for (int y = -radiusInt; y <= radiusInt; y++)
                {
                    Vector3Int cellPos = centerCell + new Vector3Int(x, y, 0);
                    Vector3 cellWorldPos = shroudedZonesTiles.GetCellCenterWorld(cellPos);

                    if (Vector3.Distance(transform.position, cellWorldPos) <= attackRange)
                    {
                        newHiddenPositions.Add(cellPos);
                    }
                }
            }

            // Restore tiles that are no longer in range
            foreach (Vector3Int pos in currentlyHiddenPositions)
            {
                if (!newHiddenPositions.Contains(pos))
                {
                    if (hiddenTiles.ContainsKey(pos))
                    {
                        shroudedZonesTiles.SetTile(pos, hiddenTiles[pos]);
                        hiddenTiles.Remove(pos);
                    }
                }
            }

            // Hide new tiles
            foreach (Vector3Int pos in newHiddenPositions)
            {
                if (!currentlyHiddenPositions.Contains(pos))
                {
                    TileBase tile = shroudedZonesTiles.GetTile(pos);
                    if (tile != null)
                    {
                        hiddenTiles[pos] = tile;
                        shroudedZonesTiles.SetTile(pos, null);
                    }
                }
            }

            currentlyHiddenPositions = newHiddenPositions;

            yield return new WaitForSeconds(checkInterval);
        }
    }

    public override void OnDeath()
    {
        base.OnDeath();
        foreach (var kvp in hiddenTiles)
        {
            shroudedZonesTiles.SetTile(kvp.Key, kvp.Value);
        }
        hiddenTiles.Clear();
        currentlyHiddenPositions.Clear();
    }

    public override void WriteStats()
    {
        Description = "";
        Skillset =
            "• Removes shrouded zones within attack range.";
        TooltipsDescription =
            "<color=yellow>Lightens the areas</color> around self, disables the effect of shrouded zones.";

        base.WriteStats();
    }
}