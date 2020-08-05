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
        public SpriteRenderer canvas;
        public GameObject innerMask;

        [Header("Painter Settings")]
        public Color penColor = Color.red;
        public int penWidth = 3;
        public LayerMask drawingLayers;
        
        public bool resetCanvasOnPlay = true;
        public bool resetCanvasOnDraw = true;
        public Color canvasClearColor = new Color(0, 0, 0, 0);  // By default, reset the canvas to be transparent
        public bool onInterruptStop = true;

        [Header("Kaleido Specifics")]
        public int kaleidoParts = 5;
        public bool mirror = true;

        // Inspector END

        public UnityEvent OnDrawFinished;

        public delegate void BrushFunc(Vector2 world_position);
        // This is the function called when a left click happens
        // Pass in your own custom one to change the brush type
        // Set the default function in the Awake method
        public BrushFunc CurrentBrush;

        // MUST HAVE READ/WRITE enabled set in the file editor of Unity
        Sprite drawableSprite;
        Texture2D drawableTex;

        Vector2 firstDragPosition;
        Vector2 prevDragPosition;
        Color[] initColors;
        Color transparent;
        Color32[] currColors;
        bool mouseHeldDownOnLastFrame = false;
        bool noDrawingOnCurrentDrag = false;

        // inputs:
        Vector2 pointerPosPS;
        bool pointerHeldDown = false;

        // New input system handler:
        public void OnPointerMove(InputAction.CallbackContext context) {

            pointerPosPS = context.ReadValue<Vector2>();
        }

        public void OnPointerHeldDown(InputAction.CallbackContext context) {
            pointerHeldDown = context.performed;
        }

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
        */



        // Default brush type. Has width and colour.
        // Pass in a point in WORLD coordinates
        // Changes the surrounding pixels of the world_point to the static pen_colour
        public void PenBrush(Vector2 pointWS) {
            Vector2 pointPS = WorldToPixelCoordinates(pointWS);
            currColors = drawableTex.GetPixels32();

            if (prevDragPosition == Vector2.zero) {
                // If this is the first time we've ever dragged on this image, simply colour the pixels at our mouse position
                if (resetCanvasOnDraw) {
                    ResetCanvas();
                }
                MarkPixelsToColour(pointPS, penWidth, penColor);
            } else {
                // Colour in a line from where we were on the last update call
                ColourBetween(prevDragPosition, pointPS, penWidth, penColor);
            }
            ApplyMarkedPixelChanges();

            //Debug.Log("Dimensions: " + pixelWidth + "," + pixelHeight + ". Units to pixels: " + unitsToPixels + ". Pixel pos: " + pixel_pos);
            prevDragPosition = pointPS;
        }

        // Used in MicroUnivese to draw kaleido pattern
        public void KaleidoBrush(Vector2 pointWS) {
            Vector2 pointPS = WorldToPixelCoordinates(pointWS);
            Vector2 centerPS = WorldToPixelCoordinates(drawableSprite.bounds.center);

            currColors = drawableTex.GetPixels32();

            if (prevDragPosition == Vector2.zero) {
                if (resetCanvasOnDraw) {
                    ResetCanvas();
                }
                // If this is the first time we've ever dragged on this image, simply colour the pixels at our mouse position
                for (int i = 0; i < kaleidoParts; ++i) {
                    Vector2 rotatedPixel = GetRotatedPixel(pointPS, centerPS, 360f / kaleidoParts * i);
                    MarkPixelsToColour(rotatedPixel, penWidth, penColor);
                    firstDragPosition = pointPS;
                }
            } else {
                // Colour in a line from where we were on the last update call
                for (int i = 0; i < kaleidoParts; ++i) {
                    Vector2 lastRotatedPixel = GetRotatedPixel(prevDragPosition, centerPS, 360f / kaleidoParts * i);
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
            prevDragPosition = pointPS;
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

            // Is the user holding down the left mouse button?
            if (pointerHeldDown && !noDrawingOnCurrentDrag) {
                // Convert mouse coordinates to world coordinates
                Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(pointerPosPS);
                // Check if the current mouse position overlaps our image
                Collider2D hit = Physics2D.OverlapPoint(mouseWorldPos, drawingLayers.value);
                if (hit != null && hit.gameObject == gameObject) {
                    // We're over the texture we're drawing on!
                    // Use whatever function the current brush is
                    CurrentBrush(mouseWorldPos);
                } else {
                    // We're not over our destination texture
                    if (onInterruptStop) {
                        noDrawingOnCurrentDrag = true;
                        if (prevDragPosition != Vector2.zero) {
                            FinishDrawing();
                        }
                    } else {
                        if (!mouseHeldDownOnLastFrame) {
                            // This is a new drag where the user is left clicking off the canvas
                            // Ensure no drawing happens until a new drag is started
                            noDrawingOnCurrentDrag = true;
                        }
                    }
                    prevDragPosition = Vector2.zero;
                }
            }
            // Mouse is released
            else if (!pointerHeldDown) {
                if (prevDragPosition != Vector2.zero) {
                    // When finished drawing:
                    FinishDrawing();
                }
                prevDragPosition = Vector2.zero;
                noDrawingOnCurrentDrag = false;
            }
            mouseHeldDownOnLastFrame = pointerHeldDown;
        }

        void FinishDrawing() {
            print("Done.");
            OnDrawFinished.Invoke();
        }


        // Set the colour of pixels in a straight line from start_point all the way to end_point, to ensure everything inbetween is coloured
        public void ColourBetween(Vector2 start_point, Vector2 end_point, int width, Color color) {
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





        public void MarkPixelsToColour(Vector2 pixel, int penThickness, Color color) {
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
        public void MarkPixelToChange(int x, int y, Color color) {
            // Need to transform x and y coordinates to flat coordinates of array
            int array_pos = y * (int)drawableSprite.rect.width + x;

            // Check if this is a valid position
            if (array_pos > currColors.Length || array_pos < 0)
                return;

            currColors[array_pos] = color;
        }
        public void ApplyMarkedPixelChanges() {
            drawableTex.SetPixels32(currColors);
            drawableTex.Apply();
        }


        public Vector2 WorldToPixelCoordinates(Vector2 world_position) {
            // Change coordinates to local coordinates of this image
            Vector3 local_pos = transform.InverseTransformPoint(world_position);

            // Change these to coordinates of pixels
            float pixelWidth = drawableSprite.rect.width;
            float pixelHeight = drawableSprite.rect.height;
            float unitsToPixels = pixelWidth / drawableSprite.bounds.size.x * transform.localScale.x;

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

            drawableSprite = GetComponent<SpriteRenderer>().sprite;
            drawableTex = drawableSprite.texture;

            // Should we reset our canvas image when we hit play in the editor?
            if (resetCanvasOnPlay)
                ResetCanvas();
        }
    }

}