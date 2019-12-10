using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Automaton
{
    [SerializeField] int size = 30;
    [SerializeField] float randomPercent;
    [SerializeField] Interval[] birthRules;
    [SerializeField] Interval[] stayRules;

    public int[,,] cells;

    // Start is called before the first frame update
    public Automaton()
    {
        CreateNew();
    }



    void FillRandom()
    {
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                for (int k = 0; k < size; k++)
                {
                    System.Random pseudoRandom = new System.Random();
                    float randomFloat = pseudoRandom.Next(0,100);
                    if (randomFloat < randomPercent)
                    {
                        cells[i, j, k] = 1;
                    }
                    else
                    {
                        cells[i, j, k] = 0;
                    }
                }
            }
        }
    }

    public void CreateNew()
    {
        cells = new int[size, size, size];
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                for (int k = 0; k < size; k++)
                {
                    cells[i, j, k] = 0;
                }
            }
        }
        FillRandom();
    }

    void NewGeneration()
    {
        int[,,] newCells = new int[size, size, size];
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                for (int k = 0; k < size; k++)
                {
                    newCells[i, j, k] = GetNewState(i, j, k);
                }
            }
        }
        cells = newCells;
    }

    int GetNewState(int i, int j, int k)
    {
        int count = CountNeighbors(i, j, k);

        if (cells[i,j,k] == 0)
        {
            foreach (Interval birthRule in birthRules)
            {
                if (birthRule.InInterval(count))
                {
                    return 1;
                }
            }
        }

        if (cells[i,j,k] == 1)
        {
            foreach (Interval stayRule in stayRules)
            {
                if (stayRule.InInterval(count))
                    return cells[i, j, k];
            }
        }

        return 0;
    }

    int CountNeighbors(int x, int y, int z)
    {
        int count = 0;
        for (int ite = -1; ite <= 1; ite++)
        {
            for (int jte = -1; jte <= 1; jte++)
            {
                for (int kte = -1; kte <= 1; kte++)
                {
                    int i = (x + ite) % size;
                    i = (i<0)?size-1:i;
                    int j = (y + jte) % size;
                    j = (j<0)?size-1:j;
                    int k = (z + kte) % size;
                    k = (k<0)?size-1:k;
                    if (i < 0 || i >= size || j < 0 || j >= size || k < 0 || k >= size)
                    {
                        count++;
                    }
                    else
                    {
                        if (i != 0 || j != 0 || k != 0)
                        {
                            count = count + (cells[i, j, k] == 1 ? 1 : 0);
                        }
                    }
                }
            }
        }
        return count;
    }

    private void OnDrawGizmos()
    {
        if (cells != null)
        {
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    for (int k = 0; k < size; k++)
                    {
                        if (cells[i,j,k] == 1)
                        {
                            Gizmos.color = Color.blue - new Color(0,0,0,0.3f);
                            Gizmos.DrawCube(new Vector3(i, j, k), Vector3.one);
                        }
                    }
                }
            }
        }
    }
}

[System.Serializable]
public struct Interval
{
    [SerializeField] int von;
    [SerializeField] int bis;

    public bool InInterval(int x)
    {
        return (x >= von && x <= bis);
    }
}