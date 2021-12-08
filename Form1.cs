using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using Microsoft.DirectX.Direct3D;
using Microsoft.DirectX;
using System.Windows.Forms;

namespace Zadanie1
{
    public partial class Form1 : Form
    {
        private Device device = null;
        private VertexBuffer vB = null;
        private IndexBuffer iB = null;
        private Vector3 camPosition, camLookAt, camUp;

        CustomVertex.PositionColored[] verts = null;

        //movement
        private float moveSpeed = 0.5f;
        private float turnSpeed = 0.1f;
        private float rotY = 0;
        private float tempY = 0;
        private float raiseConst = 0.05f;

        private float tempXZ = 0;
        private float rotXZ = 0;

        private FillMode fillMode = FillMode.WireFrame;
        private Color backGroundColor = Color.Black;

        private bool invalidating = true;

        //terrain - later gonna depend with png length and width
        private static int terWidth = 200;
        private static int terLength = 200;
        private static int vertCount = terWidth * terLength;
        private static int indCount = (terWidth - 1) * (terLength - 1) * 6;
        private static int[] indices = null;

        //bools for mouse
        bool isMiddleMouseDown = false;
        bool isLeftMouseDown = false;
        bool isRightMouseDown = false;

        private Bitmap map = null;

        public Form1()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.Opaque, true);

            InitializeComponent();
            InitializeGraphics();
            InitializeEventHandler();
        }

        private void InitializeGraphics()
        {

            PresentParameters pP = new PresentParameters();
            pP.Windowed = true;
            pP.SwapEffect = SwapEffect.Discard;
            pP.EnableAutoDepthStencil = true;
            pP.AutoDepthStencilFormat = DepthFormat.D16;

            device = new Device(0, DeviceType.Hardware, this, CreateFlags.HardwareVertexProcessing, pP);

            GenerateVertex();
            GenerateIndex();


            vB = new VertexBuffer(typeof(CustomVertex.PositionColored), vertCount, device, Usage.Dynamic | Usage.WriteOnly, CustomVertex.PositionColored.Format, Pool.Default);
            OnVertexBufferCreate(vB, null);

            iB = new IndexBuffer(typeof(int), indCount, device, Usage.WriteOnly, Pool.Default);
            OnIndexBufferCreate(iB, null);


            //camera position / movement
            camPosition = new Vector3(2, 4.5f, -3.5f);
            camLookAt = new Vector3(2, 3.5f, -2.5f);
            camUp = new Vector3(0, 1, 0);

        }
        private void InitializeEventHandler()
        {
            vB.Created += new EventHandler(OnVertexBufferCreate);
            iB.Created += new EventHandler(OnIndexBufferCreate);

            this.KeyDown += new KeyEventHandler(OnKeyDown);
            this.MouseWheel += new MouseEventHandler(OnMouseScroll);

            this.MouseMove += new MouseEventHandler(OnMouseMove);
            this.MouseDown += new MouseEventHandler(OnMouseDown);
            this.MouseUp += new MouseEventHandler(OnMouseUp);
        }
        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, backGroundColor, 1, 0);

            SetupCamera();

            device.BeginScene();

            device.VertexFormat = CustomVertex.PositionColored.Format;

            device.SetStreamSource(0, vB, 0);
            device.Indices = iB;

            device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, vertCount, 0, indCount / 3); //draw trianagle

            device.EndScene();
            device.Present();

            menuStrip1.Update();

            if (invalidating)
            {
                this.Invalidate();
            }


        }
        private void SetupCamera()
        {
            // Camera looks where we rotate
            camLookAt.X = (float)Math.Sin(rotY) + camPosition.X + (float)(Math.Sin(rotXZ) * Math.Sin(rotY));
            camLookAt.Y = (float)Math.Sin(rotXZ) + camPosition.Y;
            camLookAt.Z = (float)Math.Cos(rotY) + camPosition.Z + (float)(Math.Sin(rotXZ) * Math.Cos(rotY));

            device.Transform.Projection = Matrix.PerspectiveFovLH((float)Math.PI / 4, this.Width / this.Height, 1.0f, 5000.0f); //tutaj trzeba bedzie zmienic zeby bylo responsywne z PNG
            device.Transform.View = Matrix.LookAtLH(camPosition, camLookAt, camUp);

            device.RenderState.Lighting = false;
            device.RenderState.CullMode = Cull.CounterClockwise;
            device.RenderState.FillMode = fillMode;
        }
        private void PickingObject(Point mouseLocation)
        {
            IntersectInformation hitLocation;

            Vector3 near, far, direction;

            near = new Vector3(mouseLocation.X, mouseLocation.Y, 0);
            far = new Vector3(mouseLocation.X, mouseLocation.Y, 100);

            near.Unproject(device.Viewport, device.Transform.Projection, device.Transform.View, device.Transform.World);
            far.Unproject(device.Viewport, device.Transform.Projection, device.Transform.View, device.Transform.World);

            direction = near - far;

            for (int i = 0; i < indCount; i += 3)
            {

                if (Geometry.IntersectTri(verts[indices[i]].Position, verts[indices[i + 1]].Position, verts[indices[i + 2]].Position, near, direction, out hitLocation))
                {
                    verts[indices[i]].Color = Color.Red.ToArgb();
                    verts[indices[i + 1]].Color = Color.Red.ToArgb();
                    verts[indices[i + 2]].Color = Color.Red.ToArgb();


                    verts[indices[i]].Position += new Vector3(0, raiseConst, 0);
                    verts[indices[i + 1]].Position += new Vector3(0, raiseConst, 0);
                    verts[indices[i + 2]].Position += new Vector3(0, raiseConst, 0);


                    vB.SetData(verts, 0, LockFlags.None);
                }

            }

        }

        #region Generate Vertex and Index
        private void GenerateVertex()
        {


            verts = new CustomVertex.PositionColored[vertCount];
            int k = 0;
            for (int z = 0; z < terWidth; z++)
            {
                for (int x = 0; x < terLength; x++)
                {
                    verts[k].Position = new Vector3(x, 0, z);
                    verts[k].Color = Color.White.ToArgb();
                    k++;
                }
            }
        }
        private void GenerateIndex()
        {
            indices = new int[indCount];

            int k = 0;
            int l = 0;

            for (int i = 0; i < indCount; i += 6)
            {
                indices[i] = k;
                indices[i + 1] = k + terLength;
                indices[i + 2] = k + terLength + 1;
                indices[i + 3] = k;
                indices[i + 4] = k + terLength + 1;
                indices[i + 5] = k + 1;

                k++;
                l++;
                if (l == terLength - 1)
                {
                    l = 0;
                    k++;
                }
            }
        }
        private void OnIndexBufferCreate(object sender, EventArgs e)
        {
            IndexBuffer buffer = (IndexBuffer)sender;
            buffer.SetData(indices, 0, LockFlags.None); //puts all indices from the int array into index buffer
        }
        private void OnVertexBufferCreate(object sender, EventArgs e)
        {
            VertexBuffer buffer = (VertexBuffer)sender;

            buffer.SetData(verts, 0, LockFlags.None); //puts all vertices from the vertex array into veertex buffer
        }
        #endregion

        #region Movement and controls
        private void OnKeyDown(object sender, KeyEventArgs e)
        {

            switch (e.KeyCode)
            {
                case (Keys.W):
                    {
                        camPosition.X += moveSpeed * (float)Math.Sin(rotY);
                        camPosition.Z += moveSpeed * (float)Math.Cos(rotY);
                        break;
                    }
                case (Keys.S):
                    {
                        camPosition.X -= moveSpeed * (float)Math.Sin(rotY);
                        camPosition.Z -= moveSpeed * (float)Math.Cos(rotY);
                        break;
                    }
                case (Keys.D):
                    {
                        camPosition.X += moveSpeed * (float)Math.Sin(rotY + Math.PI / 2);
                        camPosition.Z += moveSpeed * (float)Math.Cos(rotY + Math.PI / 2);
                        break;
                    }
                case (Keys.A):
                    {
                        camPosition.X -= moveSpeed * (float)Math.Sin(rotY + Math.PI / 2);
                        camPosition.Z -= moveSpeed * (float)Math.Cos(rotY + Math.PI / 2);
                        break;
                    }
                case (Keys.Q):
                    {
                        rotY -= turnSpeed;
                        break;
                    }
                case (Keys.E):
                    {
                        rotY += turnSpeed;
                        break;
                    }
                case (Keys.Up):
                    {
                        if (rotXZ < Math.PI / 2)
                        {
                            rotXZ += turnSpeed;
                        }
                        break;
                    }
                case (Keys.Down):
                    {
                        if (rotXZ > -Math.PI / 2)
                        {
                            rotXZ -= turnSpeed;
                        }
                        break;
                    }
            }
        }
        private void OnMouseScroll(object sender, MouseEventArgs e)
        {
            camPosition.Y -= e.Delta * 0.001f;
        }
        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case (MouseButtons.Middle):
                    {
                        tempY = rotY - e.X * turnSpeed;
                        tempXZ = rotXZ + e.Y * turnSpeed / 4;
                        isMiddleMouseDown = true;
                        break;
                    }
                case (MouseButtons.Left):
                    {
                        isLeftMouseDown = true;
                        Point mouseDownLocation = new Point(e.X, e.Y);
                        PickingObject(mouseDownLocation);
                        break;
                    }
                case (MouseButtons.Right):
                    {
                        isRightMouseDown = true;
                        raiseConst = -0.05f;
                        Point mouseDownLocation = new Point(e.X, e.Y);
                        PickingObject(mouseDownLocation);
                        break;
                    }
            }
        }
        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (isMiddleMouseDown)
            {
                rotY = tempY + e.X * turnSpeed;

                float temp = tempXZ - e.Y * turnSpeed / 4;

                if (temp < Math.PI / 2 && temp > -Math.PI / 2)
                { rotXZ = temp; }
            }
            if (isLeftMouseDown)
            {
                Point mouseMoveLocation = new Point(e.X, e.Y);
                PickingObject(mouseMoveLocation);

            }
            if(isRightMouseDown)
            {
                isRightMouseDown = true;
                raiseConst = -0.05f;
                Point mouseMoveLocation = new Point(e.X, e.Y);
                PickingObject(mouseMoveLocation);
                
            }
        }
        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            switch(e.Button)
            {
                case (MouseButtons.Middle):
                    {
                        isMiddleMouseDown = false;
                        break;
                    }
                case (MouseButtons.Left):
                    {
                        isLeftMouseDown = false;
                        break;
                    }
                case (MouseButtons.Right):
                    {
                        isRightMouseDown = false;
                        raiseConst = 0.05f;
                       break;
                    }

            }
        }
#endregion

        #region Menu options   
        private void solidToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fillMode = FillMode.Solid;
            solidToolStripMenuItem.Checked = true;
            wireframeToolStripMenuItem.Checked = false;
            pointToolStripMenuItem.Checked = false;
        }

        private void wireframeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fillMode = FillMode.WireFrame;
            solidToolStripMenuItem.Checked = false;
            wireframeToolStripMenuItem.Checked = true;
            pointToolStripMenuItem.Checked = false;
        }

        private void pointToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fillMode = FillMode.Point;
            solidToolStripMenuItem.Checked = false;
            wireframeToolStripMenuItem.Checked = false;
            pointToolStripMenuItem.Checked = true;
        }

        private void bufferColorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ColorDialog cD = new ColorDialog();

            invalidating = false;
            if(cD.ShowDialog(this) ==DialogResult.OK)
            {
                backGroundColor = cD.Color;
            }
            invalidating = true;
            this.Invalidate();
        }
#endregion

        #region Saving and loading map
        private void LoadMap()
        {


            verts = new CustomVertex.PositionColored[vertCount];
            int k = 0;

            using(OpenFileDialog openFileDialog=new OpenFileDialog())
            {
                openFileDialog.Title = "Load Map";
                openFileDialog.Filter = "Bitmap File (*.bmp)|*.bmp";
                openFileDialog.InitialDirectory = Application.StartupPath;
                if(openFileDialog.ShowDialog(this)==DialogResult.OK)
                {
                    map = new Bitmap(openFileDialog.FileName);
                    Color pixelColor;

                    for (int z = 0; z < terWidth; z++)
                    {
                        for (int x = 0; x < terLength; x++)
                        {
                            if (map.Size.Width > x && map.Size.Height > z) 
                            { 
                                pixelColor = map.GetPixel(x, z);
                                verts[k].Position = new Vector3(x, (float)(pixelColor.B / 15 - 10), z); // to see difference in 3D grid
                                verts[k].Color = pixelColor.ToArgb();
                                
                            }
                            else
                            {

                                verts[k].Position = new Vector3(x, 0, z);
                                verts[k].Color = Color.White.ToArgb();
                            }
                            k++;
                        }
                    }
                }
            }


        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {

            verts = new CustomVertex.PositionColored[vertCount];
            int k = 0;

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Title = "Save Map";
                saveFileDialog.Filter = "Bitmap File (*.bmp)|*.bmp";
                saveFileDialog.InitialDirectory = Application.StartupPath;
                if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
                {

                    map = new System.Drawing.Bitmap(device.Viewport.Width,device.Viewport.Height);
                    //map.Save("New_Map",System.Drawing.Imaging.ImageFormat.Bmp);
                    
                }
            }


        }

        private void resetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GenerateVertex();
            
            vB.SetData(verts, 0, LockFlags.None);
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadMap();

            vB.SetData(verts, 0, LockFlags.None);
        }
#endregion


    }

}
