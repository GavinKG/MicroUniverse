using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace MicroUniverse {
    public class KaleidoPainter : MonoBehaviour {

        // Inspector:

        [Header("Ref")]
        public GameObject outerMask;
        public GameObject canvas;
        public GameObject innerMask;
        // outerMask -> canvas -> innerMask can be considered as a successful draw.

        [Header("Process")]
        public LayerMask useLayer;

        [Header("Painter Settings")]
        public Color penColor = Color.red;
        public int penWidth = 3;
        public LayerMask drawingLayers;

        public float innerRadius; // in metrics
        public float outerRadius; // in metrics

        public bool resetCanvasOnPlay = true;
        public bool resetCanvasOnDraw = true;
        public Color canvasClearColor = new Color(0, 0, 0, 0);  // By default, reset the canvas to be transparent

        [Header("Kaleido Specifics")]
        public int kaleidoParts = 5;
        public bool mirror = true;

        // Inspector END

        public UnityEvent OnDrawFinished;
        public UnityEvent OnDrawFailed;

        public delegate void BrushFunc(Vector2 world_position);
        // This is the function called when a left click happens
        // Pass in your own custom one to change the brush type
        // Set the default function in the Awake method
        public BrushFunc CurrentBrush;

        // MUST HAVE READ/WRITE enabled set in the file editor of Unity
        Sprite drawableSprite;
        Texture2D drawableTex;

        Vector2 firstDragPosition;
        Vector2 prevMousePosPS;
        Color[] initColors;
        Color32[] currColors;

        // inputs:
        Vector2 pointerPosPS;
        bool pointerHeldDown = false;

        bool failedTriggeredLastFrame = false;

        // New input system handler:
        public void OnPointerMove(InputAction.CallbackContext context) {

            pointerPosPS = context.ReadValue<Vector2>();
        }

        public void OnPointerHeldDown(InputAction.CallbackContext context) {
            pointerHeldDown = context.performed;
        }
        // New input system handler END:

        public enum State {
            Idle, InnerMask, Drawing, OuterMask
        }

        public State currState = State.Idle; // public for debug.


        public Texture2D GetTexture() {
            return drawableTex;
        }

        //////////////////////////////////////////////////////////////////////////////
        // BRUSH TYPES. Implement your own here


        // When you want to make your own type of brush effects,
        // Copy, paste and rename this function.
        // Go through each step
        /*
        public void BrushTemplate(Vector2 world_position) {
            // 1. Change world position to pixel coordinates
            Vector2 pixelPosition = WorldToPixelCoordinates(world_position);

            // 2. Make sure our variable for pixel array is updated in this frame
            cur_colors = drawableTex.GetPixels32();

            ////////////////////////////////////////////////////////////////
            // FILL IN CODE BELOW HERE

            // Do we care about the user left clicking and dragging?
            // If you don't, simply set the below if statement to be:
            //if (true)

            // If you do care about dragging, use the below if/else structure
            if (previous_drag_position == Vector2.zero) {
                // THIS IS THE FIRST CLICK
                // FILL IN WHATEVER YOU WANT TO DO HERE
                // Maybe mark multiple pixels to colour?
                MarkPixelsToColour(pixelPosition, penWidth, penColor);
            } else {
                // THE USER IS DRAGGING
                // Should we do stuff between the previous mouse position and the current one?
                ColourBetween(previous_drag_position, pixelPosition, penWidth, penColor);
            }
            ////////////////////////////////////////////////////////////////

            // 3. Actually apply the changes we marked earlier
            // Done here to be more efficient
            ApplyMarkedPixelChanges();

            // 4. If dragging, update where we were previously
            previous_drag_position = pixelPosition;
        }

        // Default brush type. Has width and colour.
        // Pass in a point in WORLD coordinates
        // Changes the surrounding pixels of the world_point to the static pen_colour
        public void PenBrush(Vector2 pointWS) {
            Vector2 pointPS = WorldToPixelCoordinates(pointWS);
            currColors = drawableTex.GetPixels32();

            if (prevMousePosPS == Vector2.zero) {
                // If this is the first time we've ever dragged on this image, simply colour the pixels at our mouse position
                if (resetCanvasOnDraw) {
                    ResetCanvas();
                }
                MarkPixelsToColour(pointPS, penWidth, penColor);
            } else {
                // Colour in a line from where we were on the last update call
                ColourBetween(prevMousePosPS, pointPS, penWidth, penColor);
            }
            ApplyMarkedPixelChanges();

            //Debug.Log("Dimensions: " + pixelWidth + "," + pixelHeight + ". Units to pixels: " + unitsToPixels + ". Pixel pos: " + pixel_pos);
            prevMousePosPS = pointPS;
        }
         */

        // Used in MicroUnivese to draw kaleido pattern
        public void KaleidoBrush(Vector2 pointWS) {
            Vector2 pointPS = WorldToCanvasPixelCoordinates(pointWS);
            Vector2 centerPS = WorldToCanvasPixelCoordinates(drawableSprite.bounds.center);

            currColors = drawableTex.GetPixels32();

            if (prevMousePosPS == Vector2.zero) {

                // If this is the first time we've ever dragged on this image, simply colour the pixels at our mouse position
                for (int i = 0; i < kaleidoParts; ++i) {
                    Vector2 rotatedPixel = GetRotatedPixel(pointPS, centerPS, 360f / kaleidoParts * i);
                    MarkPixelsToColour(rotatedPixel, penWidth, penColor);
                    firstDragPosition = pointPS;
                }
            } else {
                // Colour in a line from where we were on the last update call
                for (int i = 0; i < kaleidoParts; ++i) {
                    Vector2 lastRotatedPixel = GetRotatedPixel(prevMousePosPS, centerPS, 360f / kaleidoParts * i);
                    Vector2 rotatedPixel = GetRotatedPixel(pointPS, centerPS, 360f / kaleidoParts * i);
                    ColourBetween(lastRotatedPixel, rotatedPixel, penWidth, penColor);
                    if (mirror) {
                        Vector2 mirrorDir = (firstDragPosition - centerPS).normalized;
                        ColourBetween(lastRotatedPixel.Mirror(centerPS, mirrorDir), rotatedPixel.Mirror(centerPS, mirrorDir), penWidth, penColor);
                    }
                }
            }
            ApplyMarkedPixelChanges();

            //Debug.Log("Dimensions: " + pixelWidth + "," + pixelHeight + ". Units to pixels: " + unitsToPixels + ". Pixel pos: " + pixel_pos);
            prevMousePosPS = pointPS;
        }

        //////////////////////////////////////////////////////////////////////////////

        public Vector2 GetRotatedPixel(Vector2 originalPos, Vector2 centerPos, float rotateDegree) {
            Vector2 dir = originalPos - centerPos;
            dir = dir.Rotate(rotateDegree);
            return dir + centerPos;
        }


        // This is where the magic happens.
        // Detects when user is left clicking, which then call the appropriate function
        void Update() {

            bool shouldTriggerFailed = false;
           
            if (pointerHeldDown) {
                Ray ray = Camera.main.ScreenPointToRay(pointerPosPS);
                RaycastHit2D hit = Physics2D.GetRayIntersection(ray, useLayer);
                if (hit.transform != null) {
                    if (hit.transform.gameObject == innerMask) {
                        if (currState == State.Idle || currState == State.InnerMask) {
                            currState = State.InnerMask;
                        } else {
                            DrawingFailed();
                        }
                    } else if (hit.transform.gameObject == canvas) {
                        if (currState == State.InnerMask && resetCanvasOnDraw) {
                            ResetCanvas();
                        }
                        if (currState == State.InnerMask) {
                            Vector2 mousePosWS = hit.point;
                            Vector2 canvasPosWS = new Vector2(canvas.transform.position.x, canvas.transform.position.y);
                            Vector2 dir = (mousePosWS - canvasPosWS).normalized;
                            mousePosWS = dir * innerRadius; // to prevent seam
                            CurrentBrush(mousePosWS);
                            currState = State.Drawing;
                        } else if (currState == State.Drawing) {
                            Vector2 mousePosWS = hit.point;
                            CurrentBrush(mousePosWS);
                        } else {
                            shouldTriggerFailed = true;
                        }

                    } else if (hit.transform.gameObject == outerMask) {
                        if (currState == State.Drawing) {

                            Vector2 mousePosWS = hit.point;
                            Vector2 canvasPosWS = new Vector2(canvas.transform.position.x, canvas.transform.position.y);
                            Vector2 dir = (mousePosWS - canvasPosWS).normalized;
                            mousePosWS = dir * outerRadius; // to prevent seam

                            CurrentBrush(mousePosWS); // last point
                            currState = State.OuterMask;
                        } else if (currState == State.OuterMask) {
                            // Do nothing
                        } else {
                            shouldTriggerFailed = true;
                        }
                    }
                } else {
                    if (currState != State.Idle) {
                        shouldTriggerFailed = true;
                    }
                }
            } else {

                if (currState != State.Idle) {
                    if (currState == State.OuterMask) {
                        DrawingSuccess();
                    } else {
                        shouldTriggerFailed = true;
                    }
                }
            }

            if (shouldTriggerFailed) {
                if (!failedTriggeredLastFrame) {
                    DrawingFailed();
                    failedTriggeredLastFrame = true;
                }
            } else {
                failedTriggeredLastFrame = false;
            }
        }

        void DrawingFailed() {
            currState = State.Idle;
            print("Failed.");
            OnDrawFailed.Invoke();
            prevMousePosPS = Vector2.zero;
        }

        void DrawingSuccess() {
            currState = State.Idle;
            print("Done.");
            OnDrawFinished.Invoke();
            prevMousePosPS = Vector2.zero;
        }


        // Set the colour of pixels in a straight line from start_point all the way to end_point, to ensure everything inbetween is coloured
        void ColourBetween(Vector2 start_point, Vector2 end_point, int width, Color color) {
            // Get the distance from start to finish
            float distance = Vector2.Distance(start_point, end_point);
            Vector2 direction = (start_point - end_point).normalized;

            Vector2 cur_position = start_point;

            // Calculate how many times we should interpolate between start_point and end_point based on the amount of time that has passed since the last update
            float lerp_steps = 1 / distance;

            for (float lerp = 0; lerp <= 1; lerp += lerp_steps) {
                cur_position = Vector2.Lerp(start_point, end_point, lerp);
                MarkPixelsToColour(cur_position, width, color);
            }
        }





        void MarkPixelsToColour(Vector2 pixel, int penThickness, Color color) {
            // Figure out how many pixels we need to colour in each direction (x and y)
            int center_x = (int)pixel.x;
            int center_y = (int)pixel.y;
            //int extra_radius = Mathf.Min(0, pen_thickness - 2);

            // penThickness represents a SQUARE.
            for (int x = center_x - penThickness; x <= center_x + penThickness; x++) {
                // Check if the X wraps around the image, so we don't draw pixels on the other side of the image
                if (x >= (int)drawableSprite.rect.width || x < 0)
                    continue;

                for (int y = center_y - penThickness; y <= center_y + penThickness; y++) {
                    MarkPixelToChange(x, y, color);
                }
            }
        }
        void MarkPixelToChange(int x, int y, Color color) {
            // Need to transform x and y coordinates to flat coordinates of array
            int array_pos = y * (int)drawableSprite.rect.width + x;

            // Check if this is a valid position
            if (array_pos > currColors.Length || array_pos < 0)
                return;

            currColors[array_pos] = color;
        }
        void ApplyMarkedPixelChanges() {
            drawableTex.SetPixels32(currColors);
            drawableTex.Apply();
        }


        Vector2 WorldToCanvasPixelCoordinates(Vector2 world_position) {
            // Change coordinates to local coordinates of this image
            Vector3 local_pos = canvas.transform.InverseTransformPoint(world_position);

            // Change these to coordinates of pixels
            float pixelWidth = drawableSprite.rect.width;
            float pixelHeight = drawableSprite.rect.height;
            float unitsToPixels = pixelWidth / drawableSprite.bounds.size.x * canvas.transform.localScale.x;

            // Need to center our coordinates
            float centered_x = local_pos.x * unitsToPixels + pixelWidth / 2;
            float centered_y = local_pos.y * unitsToPixels + pixelHeight / 2;

            // Round current mouse position to nearest pixel
            Vector2 pixel_pos = new Vector2(Mathf.RoundToInt(centered_x), Mathf.RoundToInt(centered_y));

            return pixel_pos;
        }


        // Changes every pixel to be the reset colour
        public void ResetCanvas() {
            print("Reset");
            // Initialize clean pixels to use
            initColors = new Color[(int)drawableSprite.rect.width * (int)drawableSprite.rect.height];
            for (int x = 0; x < initColors.Length; x++) {
                initColors[x] = canvasClearColor;
            }

            drawableTex.SetPixels(initColors);

            drawableTex.Apply();
        }

        void Start() {
            Init();
        }

        public void Init() {

            // DEFAULT BRUSH SET HERE
            CurrentBrush = KaleidoBrush;

            drawableSprite = canvas.GetComponent<SpriteRenderer>().sprite;
            drawableTex = drawableSprite.texture;

            // Should we reset our canvas image when we hit play in the editor?
            if (resetCanvasOnPlay) {
                ResetCanvas();
            }
        }
    }

}