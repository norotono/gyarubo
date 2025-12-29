using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BoardManager : MonoBehaviour
{
    [Header("--- Map Settings ---")]
    public GameObject tilePrefab;
    public Transform boardParent;
    public Transform playerPiece;
    public int totalTiles = 48;

    // �O�����猻�݂̃}�b�v�����Q�Ƃ��邽�߂̃v���p�e�B
    public string[] BoardLayout { get; private set; }

    // ����������
    public void InitializeBoard(int currentGrade)
    {
        // �����̃}�X�̍폜
        foreach (Transform child in boardParent) Destroy(child.gameObject);

        BoardLayout = new string[totalTiles];

        // 1. �Œ�}�X�̔z�u
        // ���C��: 1�N���̎�����0�Ԃ�Start�ɂ���B����ȊO��Normal�i�ʉߓ_�j
        BoardLayout[0] = (currentGrade == 1) ? "Start" : "Normal";

        BoardLayout[24] = "Middle"; // ���Ԓn�_

        // �w�N���Ƃ̃S�[���E�V���b�v�z�u
        if (currentGrade == 3)
        {
            BoardLayout[totalTiles - 1] = "Goal";
            BoardLayout[totalTiles - 2] = "Shop";
        }
        else
        {
            BoardLayout[totalTiles - 1] = "Shop";
        }

        // �����}�X�̔z�u (2�N���ȏ�)
        if (currentGrade >= 2)
        {
            int[] cIdx = { 5, 13, 21, 29, 37, 45 };
            foreach (int i in cIdx)
            {
                if (i < totalTiles && string.IsNullOrEmpty(BoardLayout[i]))
                    BoardLayout[i] = "Classroom";
            }
        }

        // 2. �����_���}�X�̔z�u
        List<string> p = new List<string>();

        if (currentGrade == 1)
        {
            AddTiles(p, "Shop", 1);
            AddTiles(p, "Male", 12); AddTiles(p, "Event", 12);
            AddTiles(p, "FriendPlus", 8); AddTiles(p, "GPPlus", 6);
            AddTiles(p, "GPMinus", 4); AddTiles(p, "FriendMinus", 2);
        }
        else
        {
            // 2,3�N���p�o�����X
            if (currentGrade == 2) AddTiles(p, "Shop", 1);
            AddTiles(p, "GPPlus", 10); AddTiles(p, "FriendPlus", 8);
            AddTiles(p, "Event", 12); AddTiles(p, "GPMinus", 4);
            AddTiles(p, "Male", 3); AddTiles(p, "FriendMinus", 2);
        }

        // �V���b�t��
        p = p.OrderBy(x => Random.value).ToList();

        // �󂫃}�X�ɖ��߂�
        int pIdx = 0;
        for (int i = 0; i < totalTiles; i++)
        {
            if (string.IsNullOrEmpty(BoardLayout[i]))
            {
                BoardLayout[i] = (pIdx < p.Count) ? p[pIdx++] : "Normal";
            }
        }

        // 3. ���o�I�Ȑ����i�X�l�C�N�z�u�j
        GenerateVisualTiles();
    }

    void AddTiles(List<string> list, string type, int count)
    {
        for (int i = 0; i < count; i++) list.Add(type);
    }

    void GenerateVisualTiles()
    {
        int columns = 8; // 1�s8�}�X
        for (int visualIndex = 0; visualIndex < totalTiles; visualIndex++)
        {
            int row = visualIndex / columns;
            int col = visualIndex % columns;

            // �X�l�C�N�ϊ�: ��s�́u�E�����v�Ȃ̂Ř_���C���f�b�N�X���v�Z���Ď擾
            int logicalIndex = (row % 2 == 0) ? visualIndex : (row * columns) + (columns - 1 - col);

            GameObject t = Instantiate(tilePrefab, boardParent);
            t.name = $"Tile_{logicalIndex}_{BoardLayout[logicalIndex]}";

            TileData td = t.GetComponent<TileData>();
            if (td != null)
            {
                td.index = logicalIndex;
                td.type = ConvertStr(BoardLayout[logicalIndex]);
                td.UpdateVisuals();
            }
        }
    }

    // �v���C���[�̋���ړ�������
    public void MovePlayerPiece(int logicalIndex)
    {
        if (boardParent.childCount <= logicalIndex) return;

        int columns = 8;
        int row = logicalIndex / columns;
        int col = logicalIndex % columns;
        int visualIndex;

        if (row % 2 == 0)
        {
            visualIndex = logicalIndex;
        }
        else
        {
            int rowStart = row * columns;
            visualIndex = rowStart + (columns - 1 - col);
        }

        if (visualIndex >= 0 && visualIndex < boardParent.childCount)
        {
            if (playerPiece != null)
                playerPiece.position = boardParent.GetChild(visualIndex).position;
        }
    }

    TileType ConvertStr(string s)
    {
        switch (s)
        {
            case "Start": return TileType.Start;
            case "Goal": return TileType.Goal;
            case "Male": return TileType.Boy;
            case "Event": return TileType.Event;
            case "Shop": return TileType.Shop;
            case "Classroom": return TileType.Classroom;
            case "FriendPlus": return TileType.Friend_Plus;
            case "FriendMinus": return TileType.Friend_Minus;
            case "GPPlus": return TileType.GP_Plus;
            case "GPMinus": return TileType.GP_Minus;
            default: return TileType.Normal;
        }
    }
}