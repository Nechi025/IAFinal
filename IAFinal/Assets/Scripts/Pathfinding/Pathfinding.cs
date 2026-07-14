using System.Collections.Generic;
using UnityEngine;

public class Pathfinding : MonoBehaviour
{
    GridManager grid;

    public Transform start;
    public Transform end;
    public List<Node> currentPath;

    void Start()
    {
        grid = FindObjectOfType<GridManager>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            FindPath(
                start.transform.position,
                end.transform.position
            );
        }
    }

    public List<Node> FindPath(Vector3 startPos, Vector3 targetPos)
    {
        Node startNode = grid.NodeFromWorldPoint(startPos);
        Node targetNode = grid.NodeFromWorldPoint(targetPos);

        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();

        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            Node currentNode = openSet[0];

            //Buscar el nodo con menor fCost
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < currentNode.fCost ||
                    (openSet[i].fCost == currentNode.fCost &&
                     openSet[i].hCost < currentNode.hCost))
                {
                    currentNode = openSet[i];
                }
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            //Llego al destino
            if (currentNode == targetNode)
            {
                //Debug.Log("Path encontrado");
                currentPath = RetracePath(startNode, targetNode);
                //Debug.Log("Nodos en path: " + currentPath.Count);
                return currentPath;
            }

            //Revisar vecinos
            foreach (Node neighbor in grid.GetNeighbors(currentNode))
            {
                //Ignorar obstaculos o nodos ya revisados
                if (!neighbor.walkable || closedSet.Contains(neighbor))
                    continue;

                int newCostToNeighbor =
                    currentNode.gCost +
                    GetDistance(currentNode, neighbor);

                //Si encontro mejor camino
                if (newCostToNeighbor < neighbor.gCost ||
                    !openSet.Contains(neighbor))
                {
                    neighbor.gCost = newCostToNeighbor;

                    neighbor.hCost =
                        GetDistance(neighbor, targetNode);

                    neighbor.parent = currentNode;

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }

        //No encontro camino
        return null;
    }

    List<Node> RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();

        Node currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }

        path.Reverse();

        return path;
    }

    int GetDistance(Node a, Node b)
    {
        int dstX = Mathf.Abs(a.gridX - b.gridX);
        int dstY = Mathf.Abs(a.gridY - b.gridY);

        //Movimiento diagonal = 14
        //Movimiento recto = 10
        if (dstX > dstY)
            return 14 * dstY + 10 * (dstX - dstY);

        return 14 * dstX + 10 * (dstY - dstX);
    }

    void OnDrawGizmos()
    {
        if (currentPath != null)
        {
            Gizmos.color = Color.black;

            foreach (Node node in currentPath)
            {
                Gizmos.DrawCube(node.worldPosition, Vector3.one * 0.5f);
            }
        }
    }
}
