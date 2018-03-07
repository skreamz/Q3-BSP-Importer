using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;

namespace SkreamingSoftware
{

    public class Q3BSPImporter : EditorWindow
    {
        [MenuItem("Plugins/Skreaming Software/Import Q3 BSP")]
        public static void Import()
        {
            GetWindow<Q3BSPImporter>("Q3 BSP Importer");
        }

        private ArrayList files = new ArrayList();

        private int selectedingrid = 0;

        private string dirpath = null;

        private bool importtextures = true;
        private bool importlightmaps = false;
        private bool addcolliders = true;
        private bool usereplacmenttex = false;
        private bool makestatic = false;
        private int tezlevel = 5;
        private Texture2D replacementtexture = null;
        private Material pmat = null;

        void OnGUI()
        {
            files.Clear();

            GUILayout.Label("Q3 BSP File Selection", EditorStyles.boldLabel);
            //EditorGUILayout.ObjectField.

            EditorGUILayout.Space();

            DirectoryInfo directory = new DirectoryInfo(Application.dataPath);
            FileInfo[] fileinfos = directory.GetFiles("*" + ".bsp", SearchOption.AllDirectories);

            string[] names = new string[fileinfos.Length];

            int count = 0;

            for(int i = 0; i < fileinfos.Length; i++)
            {
                FileInfo finfo = fileinfos[i];

                if(finfo == null)
                    continue;

                names[count++] = finfo.Name;
                files.Add(finfo.FullName);

                if(i == selectedingrid)
                    dirpath = finfo.Directory.ToString();

                //if(dirpath != null)
                //Debug.Log(dirpath);
            }

            dirpath = dirpath.Replace("\\", "/");
            dirpath = dirpath.Replace(Application.dataPath, "Assets");
            //Debug.Log(dirpath);
            //Debug.Log(Application.dataPath);

            if(count != 0)
            {
                selectedingrid = GUILayout.SelectionGrid(selectedingrid, names, 3);
            }
            else
            {
                GUIStyle s = new GUIStyle();
                s.normal.textColor = Color.red;
                s.wordWrap = true;

                GUILayout.Label("No BSP files found. please make sure you have extraacted them from the pk3 file and have imported the file/folder to Unity. e.g. can be found in assets.", s);
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            GUILayout.Label("Import Options", EditorStyles.boldLabel);

            EditorGUILayout.Space();

            importtextures = GUILayout.Toggle(importtextures, "Import Textures");

            EditorGUILayout.Space();

            importlightmaps = GUILayout.Toggle(importlightmaps, "Import Lightmaps");

            EditorGUILayout.Space();

            addcolliders = GUILayout.Toggle(addcolliders, "Add Collider/s");

            if(addcolliders)
            {
                pmat = EditorGUILayout.ObjectField("Material", pmat, typeof(Material)) as Material;

            }

            EditorGUILayout.Space();

            usereplacmenttex = GUILayout.Toggle(usereplacmenttex, "Use Replacement Texture for Non Found");

            if(usereplacmenttex)
                replacementtexture = EditorGUILayout.ObjectField("Replacement Texture", replacementtexture, typeof(Texture2D)) as Texture2D;

            EditorGUILayout.Space();

            makestatic = GUILayout.Toggle(makestatic, "Make objects static");

            EditorGUILayout.Space();

            tezlevel = EditorGUILayout.IntSlider("Tesselation Level", tezlevel, 3, 50);

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            if(GUILayout.Button("Import with selected settings."))
            {
                ImportQ3BSP(files[selectedingrid].ToString());
            }
        }

        private struct Lump
        {
            public int offset;
            public int length;
        }

        private struct TextureInfo
        {
            public string name; // Texture name. length 64
            public int flags; //   Surface flags.
            public int contents; //    Content flags.

            public Texture2D texture;
        }

        private struct Verts
        {
            public Vector3 position; //  Vertex position.
            public Vector2[] texcoord; // Vertex texture coordinates. 0=surface, 1=lightmap.
            public Vector3 normal; //Vertex normal.
            public byte[] color; //  Vertex color. RGBA. 4 bytes
        }

        private struct Face
        {

            public int texture; // Texture index.
            public int effect; //  Index into lump 12 (Effects), or -1.
            public int type; //    Face type. 1=polygon, 2=patch, 3=mesh, 4=billboard
            public int vertex; //  Index of first vertex.
            public int numvertexes; //  Number of vertices.
            public int meshvert; //    Index of first meshvert.
            public int nummeshverts; // Number of meshverts.
            public int lmapindex; //    Lightmap index.
            public Vector2Int lmapstart; // Corner of this face's lightmap image in lightmap. [2]
            public Vector2Int lmapsize; //  Size of this face's lightmap image in lightmap. [2]
            public Vector3 lmaporigin; // World space origin of lightmap. [3]
            public Vector3[] lmapvecs; // World space lightmap s and t unit vectors. [2][3]
            public Vector3 normal; // Surface normal.[3]
            public Vector2Int size; //Patch dimensions.[2]
        }

        TextureInfo[] texinfo = null;
        Verts[] vertices = null;
        int[] meshverts = null;
        Face[] faces = null;
        Texture2D[] lightmaps = null;

        int facecount = 0;

        private void ImportQ3BSP(string filename)
        {
            BinaryReader br = new BinaryReader(File.OpenRead(filename));

            facecount = 0;

            if(br == null)
            {
                Debug.LogError("Unable to open " + filename);
                return;
            }

            char[] magic = br.ReadChars(4);

            if(new string(magic) != "IBSP")
            {
                Debug.LogError("not a valid BSP file, magic = " + new string(magic));
                return;
            }
            else
                Debug.Log("Valid BSP file, magic = " + new string(magic));

            int version = br.ReadInt32();

            if(version != 0x2e)
            {
                Debug.LogError("not a Q3 BSP file, version = " + version);
                return;
            }
            else
                Debug.Log("Q3 BSP file, version = " + version + " Correct");

            Lump[] lumps = new Lump[17];

            for(int i = 0; i < 17; i++)
            {
                lumps[i].offset = br.ReadInt32();
                lumps[i].length = br.ReadInt32();
            }

            LoadEntityLump(br, lumps[0]);
            LoadTextureLump(br, lumps[1]);
            LoadVerticesLump(br, lumps[10]);
            LoadMeshVertsLump(br, lumps[11]);
            LoadFacesLump(br, lumps[13]);
            LoadLightmapLump(br, lumps[14]);

            CreateMapGameObject();

            br.Close();

            texinfo = null;
            vertices = null;
            meshverts = null;
            faces = null;
            lightmaps = null;

            GC.Collect();
        }

        private void LoadEntityLump(BinaryReader br, Lump lump) //prints to console
        {
            br.BaseStream.Position = lump.offset;

            Debug.Log(new string(br.ReadChars(lump.length)));
        }

        private void LoadTextureLump(BinaryReader br, Lump lump)
        {
            br.BaseStream.Position = lump.offset;

            int texstructsize = (sizeof(byte) * 64) + (sizeof(int) * 2);

            int size = lump.length / texstructsize;

            texinfo = new TextureInfo[size];

            string texpath = dirpath.Substring(0, dirpath.Length - 4);

            for(int i = 0; i < size; i++)
            {
                texinfo[i].name = new string(br.ReadChars(64)).Replace("\0", string.Empty);
                texinfo[i].flags = br.ReadInt32();
                texinfo[i].contents = br.ReadInt32();

                if(texinfo[i].name.EndsWith("_trans"))
                    texinfo[i].name = texinfo[i].name.Remove(texinfo[i].name.Length - 6, 6);

                if(texinfo[i].name.EndsWith("flat_400"))
                    texinfo[i].name = texinfo[i].name.Remove(texinfo[i].name.Length - 8, 8);

                if(texinfo[i].name.EndsWith("_750"))
                    texinfo[i].name = texinfo[i].name.Remove(texinfo[i].name.Length - 4, 4);

                if(texinfo[i].name.EndsWith("_1K"))
                    texinfo[i].name = texinfo[i].name.Remove(texinfo[i].name.Length - 3, 3);

                if(texinfo[i].name.EndsWith("_5K"))
                    texinfo[i].name = texinfo[i].name.Remove(texinfo[i].name.Length - 3, 3);

                texinfo[i].texture = (Texture2D)AssetDatabase.LoadAssetAtPath(texpath + texinfo[i].name + ".jpg", typeof(Texture2D));

                if(texinfo[i].texture == null)
                {
                    texinfo[i].texture = (Texture2D)AssetDatabase.LoadAssetAtPath(texpath + texinfo[i].name + ".tga", typeof(Texture2D));

                    if(texinfo[i].texture == null)
                    {
                        if(usereplacmenttex && replacementtexture != null)
                        {
                            texinfo[i].texture = replacementtexture;

                            Debug.Log("Using replacement texture");
                        }
                        else
                        {
                            Debug.Log("Unable to Find " + (texpath + texinfo[i].name));
                        }
                    }
                }
            }
        }

        private void LoadVerticesLump(BinaryReader br, Lump lump)
        {
            br.BaseStream.Position = lump.offset;

            int vertstructsize = (sizeof(byte) * 4) + (sizeof(float) * 10);

            int size = lump.length / vertstructsize;

            vertices = new Verts[size];

            for(int i = 0; i < size; i++)
            {
                vertices[i].position = new Vector3();
                vertices[i].position.x = br.ReadSingle();
                vertices[i].position.z = -br.ReadSingle();
                vertices[i].position.y = br.ReadSingle();

                vertices[i].texcoord = new Vector2[2];

                vertices[i].texcoord[0] = new Vector2();
                vertices[i].texcoord[0].x = 1f - br.ReadSingle();
                vertices[i].texcoord[0].y = 1f - br.ReadSingle();
                vertices[i].texcoord[1] = new Vector2();
                vertices[i].texcoord[1].x = br.ReadSingle();
                vertices[i].texcoord[1].y = br.ReadSingle();

                vertices[i].normal = new Vector3();
                vertices[i].normal.x = br.ReadSingle();
                vertices[i].normal.z = -br.ReadSingle();
                vertices[i].normal.y = br.ReadSingle();

                vertices[i].color = br.ReadBytes(4);
            }
        }

        private void LoadMeshVertsLump(BinaryReader br, Lump lump)
        {
            br.BaseStream.Position = lump.offset;

            int mvstructsize = sizeof(int);

            int size = lump.length / mvstructsize;

            meshverts = new int[size];

            for(int i = 0; i < size; i++)
                meshverts[i] = br.ReadInt32();
        }

        private void LoadFacesLump(BinaryReader br, Lump lump)
        {
            br.BaseStream.Position = lump.offset;

            int facestructsize = (sizeof(int) * 14) + (sizeof(float) * 12);

            int size = lump.length / facestructsize;

            faces = new Face[size];

            for(int i = 0; i < size; i++)
            {
                faces[i].texture = br.ReadInt32();
                faces[i].effect = br.ReadInt32();
                faces[i].type = br.ReadInt32();
                faces[i].vertex = br.ReadInt32();
                faces[i].numvertexes = br.ReadInt32();
                faces[i].meshvert = br.ReadInt32();
                faces[i].nummeshverts = br.ReadInt32();
                faces[i].lmapindex = br.ReadInt32();

                faces[i].lmapstart = new Vector2Int();
                faces[i].lmapstart.x = br.ReadInt32();
                faces[i].lmapstart.y = br.ReadInt32();

                faces[i].lmapsize = new Vector2Int();
                faces[i].lmapsize.x = br.ReadInt32();
                faces[i].lmapsize.y = br.ReadInt32();

                faces[i].lmaporigin = new Vector3();
                faces[i].lmaporigin.x = br.ReadSingle();
                faces[i].lmaporigin.z = -br.ReadSingle();
                faces[i].lmaporigin.y = br.ReadSingle();

                faces[i].lmapvecs = new Vector3[2];
                faces[i].lmapvecs[0] = new Vector3();
                faces[i].lmapvecs[0].x = br.ReadSingle();
                faces[i].lmapvecs[0].z = -br.ReadSingle();
                faces[i].lmapvecs[0].y = br.ReadSingle();
                faces[i].lmapvecs[1] = new Vector3();
                faces[i].lmapvecs[1].x = br.ReadSingle();
                faces[i].lmapvecs[1].z = -br.ReadSingle();
                faces[i].lmapvecs[1].y = br.ReadSingle();

                faces[i].normal = new Vector3();
                faces[i].normal.x = br.ReadSingle();
                faces[i].normal.z = -br.ReadSingle();
                faces[i].normal.y = br.ReadSingle();

                faces[i].size = new Vector2Int();
                faces[i].size.x = br.ReadInt32();
                faces[i].size.y = br.ReadInt32();


            }
        }

        private void LoadLightmapLump(BinaryReader br, Lump lump)
        {
            br.BaseStream.Position = lump.offset;

            int lmstructsize = sizeof(byte) * 128 * 128 * 3;

            int size = lump.length / lmstructsize;

            lightmaps = new Texture2D[size];

            for(int i = 0; i < size; i++)
            {
                byte[] barray = br.ReadBytes(128 * 128 * 3);

                Texture2D t2d = new Texture2D(128, 128, TextureFormat.RGBA32, false);

                for(int j = 0, x = 0, y = 0; j < 128 * 128 * 3; j += 3)
                {
                    t2d.SetPixel(x, y, new Color32(barray[j], barray[j + 1], barray[j + 2], 255));

                    x++;

                    if(x >= 255)
                    {
                        x = 0;
                        y++;
                    }
                }

                t2d.Apply();
                lightmaps[i] = t2d;
            }
        }

        private void CreateMapGameObject()
        {
            GameObject top = new GameObject("BSP");

            if(makestatic)
                top.isStatic = true;

            foreach(Face f in faces)
            {
                if(f.type == 2)
                {
                    CreateBezierCurveObject(f, top);
                    continue;
                }

                if(f.type == 4)
                {
                    //CreateBillboardObject(f, top);
                    continue;
                }

                GameObject go = new GameObject("Face " + facecount++);

                if(makestatic)
                    go.isStatic = true;

                go.transform.parent = top.transform;

                MeshFilter mf = go.AddComponent<MeshFilter>();
                go.AddComponent<MeshRenderer>();

                Mesh mesh = new Mesh();

                int offset = f.vertex;

                List<Vector3> v3l = new List<Vector3>();
                List<Vector2> v2t = new List<Vector2>();
                List<Vector2> v2l = new List<Vector2>();

                for(int i = 0; i < f.numvertexes; i++)
                {
                    v3l.Add(vertices[offset].position);
                    v2t.Add(vertices[offset].texcoord[0]);
                    v2l.Add(vertices[offset].texcoord[1]);

                    offset++;
                }

                List<int> mv = new List<int>();

                offset = f.meshvert;

                for(int i = 0; i < f.nummeshverts; i++)
                {
                    mv.Add(meshverts[offset + i]);
                }

                mv.Reverse();

                mesh.vertices = v3l.ToArray();
                mesh.uv = v2t.ToArray();
                mesh.uv2 = v2l.ToArray();
                mesh.triangles = mv.ToArray();

                mesh.RecalculateBounds();
                mesh.RecalculateNormals();

                mf.mesh = mesh;

                Material mat;

                if(f.lmapindex >= 0 && importlightmaps)
                    mat = new Material(Shader.Find("Legacy Shaders/Lightmapped/Diffuse"));
                else
                    mat = new Material(Shader.Find("Diffuse"));

                if(importtextures)
                    mat.mainTexture = texinfo[f.texture].texture;

                if(importlightmaps)
                {
                    if(f.lmapindex >= 0)
                        mat.SetTexture("_LightMap", lightmaps[f.lmapindex]);
                }

                go.GetComponent<MeshRenderer>().material = mat;

                if(addcolliders)
                    go.AddComponent<MeshCollider>();
            }
        }

        void CreateBezierCurveObject(Face f, GameObject top)
        {
            int numpatches = ((f.size[0] - 1) / 2) * ((f.size[1] - 1) / 2);

            Vector3[,] verts = new Vector3[f.size[0], f.size[1]];
            Vector2[,] uv1s = new Vector2[f.size[0], f.size[1]];
            Vector2[,] uv2s = new Vector2[f.size[0], f.size[1]];

            for(int j = 0, x = 0, y = 0; j < f.numvertexes; j++)
            {
                verts[x, y] = vertices[f.vertex + j].position;
                uv1s[x, y] = vertices[f.vertex + j].texcoord[0];
                uv2s[x++, y] = vertices[f.vertex + j].texcoord[1];

                if(x == f.size[0])
                {
                    y++;
                    x = 0;
                }
            }

            int gridlevelx = 0;
            int gridlevely = 0;

            List<Vector3> vs = new List<Vector3>();
            List<Vector2> us = new List<Vector2>();
            List<Vector2> u2s = new List<Vector2>();
            List<int> index = new List<int>();

            for(int i = 0; i < numpatches; i++)
            {
                List<Vector3> verts2 = new List<Vector3>();
                List<Vector2> uv1s2 = new List<Vector2>();
                List<Vector2> uv2s2 = new List<Vector2>();

                for(int j = 0; j < 3; j++)
                {
                    verts2.Add(verts[(gridlevelx * 2), (gridlevely * 2) + j]);
                    verts2.Add(verts[(gridlevelx * 2) + 1, (gridlevely * 2) + j]);
                    verts2.Add(verts[(gridlevelx * 2) + 2, (gridlevely * 2) + j]);

                    //

                    uv1s2.Add(uv1s[(gridlevelx * 2), (gridlevely * 2) + j]);
                    uv1s2.Add(uv1s[(gridlevelx * 2) + 1, (gridlevely * 2) + j]);
                    uv1s2.Add(uv1s[(gridlevelx * 2) + 2, (gridlevely * 2) + j]);

                    //

                    uv2s2.Add(uv2s[(gridlevelx * 2), (gridlevely * 2) + j]);
                    uv2s2.Add(uv2s[(gridlevelx * 2) + 1, (gridlevely * 2) + j]);
                    uv2s2.Add(uv2s[(gridlevelx * 2) + 2, (gridlevely * 2) + j]);
                }
                
                int vcount = vs.Count;

                vs.AddRange(Tessellate3(tezlevel, verts2));
                us.AddRange(Tessellate2(tezlevel, uv1s2));
                u2s.AddRange(Tessellate2(tezlevel, uv2s2));


                for(int k = 0; k < ((tezlevel + 1) * (tezlevel + 1)) - (tezlevel + 1); k++)
                {
                    if(k == 0 || (k % (tezlevel + 1) == 0 && k > tezlevel))
                    {
                        index.Add(k + vcount);
                        index.Add(k + (tezlevel + 1) + vcount);
                        index.Add(k + 1 + vcount);

                        continue;
                    }
                    else if(k == tezlevel || (k % (tezlevel + 1) == tezlevel && k > tezlevel + 1))
                    {
                        index.Add(k + vcount);
                        index.Add(k + ((tezlevel + 1) - 1) + vcount);
                        index.Add(k + (tezlevel + 1) + vcount);

                        continue;
                    }
                    else
                    {
                        index.Add(k + vcount);
                        index.Add(k + ((tezlevel + 1) - 1) + vcount);
                        index.Add(k + (tezlevel + 1) + vcount);

                        index.Add(k + vcount);
                        index.Add(k + (tezlevel + 1) + vcount);
                        index.Add(k + 1 + vcount);

                        continue;
                    }
                }

                gridlevelx++;

                if(gridlevelx >= (f.size[0] - 1) / 2)
                {
                    gridlevelx = 0;
                    gridlevely++;
                }
            }

            index.Reverse();

            Mesh mesh = new Mesh();

            mesh.vertices = vs.ToArray();
            mesh.triangles = index.ToArray();
            mesh.uv = us.ToArray();
            mesh.uv2 = u2s.ToArray();

            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            GameObject go = new GameObject("Face Bez " + facecount++);

            if(makestatic)
                go.isStatic = true;

            go.transform.parent = top.transform;

            MeshFilter mf = go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>();

            mf.mesh = mesh;

            Material mat;

            if(f.lmapindex >= 0 && importlightmaps)
                mat = new Material(Shader.Find("Legacy Shaders/Lightmapped/Diffuse"));
            else
                mat = new Material(Shader.Find("Diffuse"));

            if(importtextures)
                mat.mainTexture = texinfo[f.texture].texture;

            if(importlightmaps)
            {
                if(f.lmapindex >= 0)
                    mat.SetTexture("_LightMap", lightmaps[f.lmapindex]);
            }

            go.GetComponent<MeshRenderer>().material = mat;

            if(addcolliders)
                go.AddComponent<MeshCollider>();
        }
        
        void CreateBillboardObject(Face f, GameObject top)
        {
            //Debug.Log("Billboard, texture = " + f.texture + " numvertices = " + f.numvertexes);

            GameObject go = new GameObject("Face Billboard " + facecount++);

            go.transform.parent = top.transform;

            MeshFilter mf = go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>();

            Mesh mesh = new Mesh();

            int offset = f.vertex;

            List<Vector3> v3l = new List<Vector3>();
            List<Vector2> v2t = new List<Vector2>();
            List<int> mv = new List<int>();

            v3l.Add(vertices[f.vertex].position + new Vector3(-0.5f, 0.5f, 0f));
            v3l.Add(vertices[f.vertex].position + new Vector3(-0.5f, -0.5f, 0f));
            v3l.Add(vertices[f.vertex].position + new Vector3(0.5f, -0.5f, 0f));
            v3l.Add(vertices[f.vertex].position + new Vector3(0.5f, 0.5f, 0f));

            v2t.Add(new Vector2(0f, 0f));
            v2t.Add(new Vector2(0f, 1f));
            v2t.Add(new Vector2(1f, 1f));
            v2t.Add(new Vector2(1f, 0f));

            mv.Add(0);
            mv.Add(1);
            mv.Add(2);
            mv.Add(3);
            mv.Add(0);
            mv.Add(1);

            //mv.Reverse();

            mesh.vertices = v3l.ToArray();
            mesh.uv = v2t.ToArray();
            mesh.triangles = mv.ToArray();

            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            mf.mesh = mesh;

            Material mat = new Material(Shader.Find("Diffuse"));

            mat.mainTexture = texinfo[f.texture].texture;

            go.GetComponent<MeshRenderer>().material = mat;
        }

        private List<Vector3> Tessellate3(int level, List<Vector3> control)
        {
            List<Vector3> vertex = new List<Vector3>();

            List<Vector3>[] vects = new List<Vector3>[3];

            for(int set = 0; set < 3; set++)
            {
                vects[set] = new List<Vector3>();

                float adval = 1.0f / level;

                vects[set].Add(control[set]);

                for(int i = 0; i < (level - 1); i++)
                {
                    vects[set].Add(new Vector3((((1f - adval) * (1f - adval)) * control[set].x) + (2 * (1f - adval)) * (adval * control[set + 3].x) + ((adval * adval) * control[set + 6].x),
                        (((1f - adval) * (1f - adval)) * control[set].y) + (2 * (1f - adval)) * (adval * control[set + 3].y) + ((adval * adval) * control[set + 6].y),
                        (((1f - adval) * (1f - adval)) * control[set].z) + (2 * (1f - adval)) * (adval * control[set + 3].z) + ((adval * adval) * control[set + 6].z)));
                    
                    adval += 1.0f / level;
                }

                vects[set].Add(control[set + 6]);
            }


            for(int range = 0; range <= level; range++)
            {
                float adval = 1.0f / level;

                vertex.Add(vects[0][range]);

                for(int i = 0; i < (level - 1); i++)
                {
                    vertex.Add(new Vector3((((1f - adval) * (1f - adval)) * vects[0][range].x) + (2 * (1f - adval)) * (adval * vects[1][range].x) + ((adval * adval) * vects[2][range].x),
                        (((1f - adval) * (1f - adval)) * vects[0][range].y) + (2 * (1f - adval)) * (adval * vects[1][range].y) + ((adval * adval) * vects[2][range].y),
                        (((1f - adval) * (1f - adval)) * vects[0][range].z) + (2 * (1f - adval)) * (adval * vects[1][range].z) + ((adval * adval) * vects[2][range].z)));

                    adval += 1.0f / level;
                }

                vertex.Add(vects[2][range]);
            }

            return vertex;
        }

        private List<Vector2> Tessellate2(int level, List<Vector2> control)
        {
            List<Vector2> vertex = new List<Vector2>();

            List<Vector2>[] vects = new List<Vector2>[3];

            for(int set = 0; set < 3; set++)
            {
                vects[set] = new List<Vector2>();

                float adval = 1.0f / level;

                vects[set].Add(control[set]);

                for(int i = 0; i < (level - 1); i++)
                {
                    vects[set].Add(new Vector2((((1f - adval) * (1f - adval)) * control[set].x) + (2 * (1f - adval)) * (adval * control[set + 3].x) + ((adval * adval) * control[set + 6].x),
                        (((1f - adval) * (1f - adval)) * control[set].y) + (2 * (1f - adval)) * (adval * control[set + 3].y) + ((adval * adval) * control[set + 6].y)));

                    adval += 1.0f / level;
                }

                vects[set].Add(control[set + 6]);
            }


            for(int range = 0; range <= level; range++)
            {
                float adval = 1.0f / level;

                vertex.Add(vects[0][range]);

                for(int i = 0; i < (level - 1); i++)
                {
                    vertex.Add(new Vector2((((1f - adval) * (1f - adval)) * vects[0][range].x) + (2 * (1f - adval)) * (adval * vects[1][range].x) + ((adval * adval) * vects[2][range].x),
                        (((1f - adval) * (1f - adval)) * vects[0][range].y) + (2 * (1f - adval)) * (adval * vects[1][range].y) + ((adval * adval) * vects[2][range].y)));

                    adval += 1.0f / level;
                }

                vertex.Add(vects[2][range]);
            }

            return vertex;
        }
    }
}