using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node
{
    public bool walkable;
    public Vector3 worldPosition;

    public int gridX;
    public int gridY;

    public int gCost; //Costo desde inicio
    public int hCost; //Distancia al objetivo

    public Node parent;

    public int fCost => gCost + hCost; //Prioridad total

    public Node(bool walkable, Vector3 worldPosition, int gridX, int gridY)
    {
        this.walkable = walkable;
        this.worldPosition = worldPosition;

        this.gridX = gridX;
        this.gridY = gridY;
    }
}
