using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

public class MouseDragSelection : MonoBehaviour
{
    private Rect rectDef = new Rect();
    public Color color = Color.green;
    private Vector3[] mousePositions = new Vector3[2];
    private bool draggingMouse = false;
    private bool drawRect = false;
    private float timer = 5f; // timeout in case of not regiestering mouse up.

    public List<Unit> LastSelectedUnits;

    // Declare the delegate (if using non-generic pattern).
    public delegate void SelectionEndEventHandler(Rect rect);
    public event SelectionEndEventHandler OnSelectionEnd;

    void OnGUI()
    {
        if (drawRect)
        {
            DrawRectangle(rectDef, 1, color);
        }
    }

    void DrawRectangle(Rect rect, int frameWidth, Color color)
    {
        //Create a one pixel texture with the right color
        var texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();

        Rect area = new Rect(rect.x, Screen.height - rect.y, rect.width, -rect.height);

        Rect lineArea = area;
        lineArea.height = frameWidth; //Top line
        GUI.DrawTexture(lineArea, texture);
        lineArea.y = area.yMax - frameWidth; //Bottom
        GUI.DrawTexture(lineArea, texture);
        lineArea = area;
        lineArea.width = frameWidth; //Left
        GUI.DrawTexture(lineArea, texture);
        lineArea.x = area.xMax - frameWidth;//Right
        GUI.DrawTexture(lineArea, texture);
    }

    void reset()
    {
        drawRect = false;
        mousePositions[0] = new Vector3();
        mousePositions[1] = new Vector3();
        timer = 5f;
        draggingMouse = false;
    }
    private void Update()
    {
        if (drawRect)
        {
            if (timer > 0.1)
            {
                timer -= 1 * Time.deltaTime;
            }
            else
            {
                reset();
            }
        }
        if (Input.GetMouseButtonDown(0))
        {
            if (!draggingMouse)
            {
                mousePositions[0] = Input.mousePosition;
            }
            draggingMouse = true;
        }
        if (Input.GetMouseButton(0))
        {
            if (draggingMouse)
            {
                mousePositions[1] = Input.mousePosition;
                float width = Math.Abs(mousePositions[1].x - mousePositions[0].x);
                float height = Math.Abs(mousePositions[1].y - mousePositions[0].y);
                rectDef = new Rect(
                    new float[] { mousePositions[0].x, mousePositions[1].x }.Min(),
                    new float[] { mousePositions[0].y, mousePositions[1].y }.Min(),
                    width,
                    height);
                drawRect = true;
            }
        }
        if (Input.GetMouseButtonUp(0))
        {
            OnSelectionEnd?.Invoke(rectDef);
            reset();
        }
    }
}