using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace OriginalKartRacer.EditorTools
{
    /// <summary>自研程序化模型：所有尺寸以米为单位，无外部 IP 模型依赖。</summary>
    public static class OriginalRaceArt
    {
        public static void BuildKart(Transform root,Material paint,Material accent,Material rubber,Material skin,Material glass)
        {
            var alloy=Material("BrushedAlloy",new Color(.65f,.69f,.72f),.7f,.7f);
            var suit=Material("RaceSuit",new Color(.12f,.16f,.21f),.25f);
            var white=Material("RaceWhite",new Color(.94f,.95f,.92f),.6f);
            Hull(root,"Chassis",new Vector3(0,-.06f,0),new Vector3(1.36f,.22f,2.5f),rubber);
            Hull(root,"SculptedNose",new Vector3(0,.13f,.74f),new Vector3(1.25f,.38f,1.25f),paint);
            Part(root,PrimitiveType.Cube,"NoseStripe",new Vector3(0,.328f,.77f),new Vector3(.15f,.015f,.75f),white);
            Hull(root,"Bumper",new Vector3(0,-.06f,1.28f),new Vector3(1.8f,.14f,.25f),accent);
            Hull(root,"RearBumper",new Vector3(0,-.02f,-1.24f),new Vector3(1.8f,.17f,.27f),accent);
            for(int side=-1;side<=1;side+=2)
            {
                Hull(root,"SidePod",new Vector3(side*.62f,.12f,-.21f),new Vector3(.42f,.37f,1.32f),paint);
                Part(root,PrimitiveType.Cube,"SideStripe",new Vector3(side*.842f,.13f,-.2f),new Vector3(.012f,.1f,.88f),white);
                for(int vent=0;vent<4;vent++)
                    Part(root,PrimitiveType.Cube,"AirVent",new Vector3(side*.846f,.24f,-.55f+vent*.11f),new Vector3(.012f,.085f,.045f),rubber);
                for(int z=-1;z<=1;z+=2)
                {
                    var wheelRoot=new GameObject(z==1?"FrontWheel":"RearWheel").transform;
                    wheelRoot.SetParent(root,false);wheelRoot.localPosition=new Vector3(side*.91f,-.035f,z*.85f);
                    var tire=Part(wheelRoot,PrimitiveType.Cylinder,"Tire",Vector3.zero,new Vector3(.63f,.16f,.63f),rubber);
                    tire.localRotation=Quaternion.Euler(0,0,90);
                    var rim=Part(wheelRoot,PrimitiveType.Cylinder,"Rim",new Vector3(side*.168f,0,0),new Vector3(.40f,.02f,.40f),alloy);
                    rim.localRotation=Quaternion.Euler(0,0,90);
                    for(int spoke=0;spoke<5;spoke++)
                    {
                        float a=spoke*Mathf.PI*2/5;
                        var arm=Part(wheelRoot,PrimitiveType.Cube,"RimSpoke",new Vector3(side*.193f,Mathf.Cos(a)*.07f,Mathf.Sin(a)*.07f),new Vector3(.012f,.22f,.035f),rubber);
                        arm.localRotation=Quaternion.Euler(a*Mathf.Rad2Deg,0,0);
                    }
                    var cap=Part(wheelRoot,PrimitiveType.Sphere,"HubCap",new Vector3(side*.203f,0,0),new Vector3(.025f,.11f,.11f),accent);
                    Tube(root,"Suspension",new Vector3(side*.4f,-.06f,z*.65f),new Vector3(side*.84f,-.03f,z*.85f),.028f,alloy);
                }
                Tube(root,"WingSupport",new Vector3(side*.53f,.1f,-1.0f),new Vector3(side*.53f,.67f,-1.0f),.035f,alloy);
                var exhaust=Part(root,PrimitiveType.Cylinder,"ExhaustPipe",new Vector3(side*.52f,.1f,-1.33f),new Vector3(.14f,.17f,.14f),alloy);
                exhaust.localRotation=Quaternion.Euler(90,0,0);
                Part(root,PrimitiveType.Sphere,"TailLamp",new Vector3(side*.71f,.17f,-1.34f),new Vector3(.22f,.09f,.04f),Material("TailRed",new Color(.9f,.06f,.015f),.7f));
                Part(root,PrimitiveType.Sphere,"HeadlampLens",new Vector3(side*.47f,.2f,1.28f),new Vector3(.22f,.11f,.045f),white);
                var lamp=new GameObject("Headlamp").AddComponent<Light>();lamp.transform.SetParent(root,false);lamp.transform.localPosition=new Vector3(side*.47f,.2f,1.33f);lamp.type=LightType.Spot;lamp.range=18;lamp.spotAngle=55;lamp.intensity=2;lamp.enabled=false;
            }
            Hull(root,"RearWing",new Vector3(0,.69f,-1.03f),new Vector3(1.94f,.105f,.44f),paint);
            Hull(root,"Seat",new Vector3(0,.3f,-.48f),new Vector3(.65f,.18f,.67f),rubber);
            var seatBack=Part(root,PrimitiveType.Cube,"BucketSeat",new Vector3(0,.64f,-.69f),new Vector3(.64f,.78f,.14f),rubber);seatBack.localRotation=Quaternion.Euler(-12,0,0);
            // 优先使用导入的 Humanoid 驾驶员。若资源尚未安装，则保留原来的程序化驾驶员作为回退。
            if(!TryAttachImportedDriver(root))
            {
                // 坐姿比例、肩膀、肘关节与脚掌形成完整车手剪影。
                var torso=Part(root,PrimitiveType.Capsule,"DriverTorso",new Vector3(0,.71f,-.42f),new Vector3(.47f,.31f,.32f),suit);torso.localRotation=Quaternion.Euler(12,0,0);
                Part(root,PrimitiveType.Sphere,"SuitChest",new Vector3(0,.75f,-.245f),new Vector3(.31f,.32f,.055f),accent);
                for(int side=-1;side<=1;side+=2)
                {
                    Vector3 shoulder=new Vector3(side*.245f,.87f,-.36f),elbow=new Vector3(side*.34f,.66f,-.08f),hand=new Vector3(side*.22f,.73f,.22f);
                    Tube(root,"UpperArm",shoulder,elbow,.085f,suit);Tube(root,"ForeArm",elbow,hand,.075f,accent);
                    Part(root,PrimitiveType.Sphere,"Glove",hand,new Vector3(.16f,.16f,.16f),white);
                    Vector3 hip=new Vector3(side*.16f,.38f,-.37f),knee=new Vector3(side*.23f,.37f,.1f),ankle=new Vector3(side*.22f,.13f,.48f);
                    Tube(root,"Thigh",hip,knee,.105f,suit);Tube(root,"Shin",knee,ankle,.09f,suit);
                    Hull(root,"Boot",ankle+Vector3.forward*.09f,new Vector3(.19f,.15f,.34f),white);
                }
                Tube(root,"Neck",new Vector3(0,.96f,-.39f),new Vector3(0,1.08f,-.36f),.095f,skin);
                Part(root,PrimitiveType.Sphere,"HelmetShell",new Vector3(0,1.27f,-.34f),new Vector3(.63f,.66f,.66f),paint);
                Part(root,PrimitiveType.Sphere,"Visor",new Vector3(0,1.28f,-.096f),new Vector3(.535f,.245f,.25f),glass);
                Part(root,PrimitiveType.Sphere,"HelmetChin",new Vector3(0,1.12f,-.105f),new Vector3(.5f,.14f,.25f),white);
                Part(root,PrimitiveType.Sphere,"HelmetStripe",new Vector3(0,1.275f,-.36f),new Vector3(.1f,.675f,.675f),white);
            }
            // 三根辐条围成方向盘，保留镂空中心。
            Vector3 center=new Vector3(0,.69f,.21f);
            for(int i=0;i<16;i++)
            {float a=i*Mathf.PI/8,b=(i+1)*Mathf.PI/8;Tube(root,"SteeringRim",center+new Vector3(Mathf.Cos(a)*.235f,Mathf.Sin(a)*.18f,Mathf.Sin(a)*.09f),center+new Vector3(Mathf.Cos(b)*.235f,Mathf.Sin(b)*.18f,Mathf.Sin(b)*.09f),.024f,rubber);}
            Tube(root,"SteeringColumn",center,new Vector3(0,.21f,.52f),.035f,alloy);
            Tube(root,"SteeringSpoke",center-Vector3.right*.2f,center+Vector3.right*.2f,.02f,alloy);
        }

        private static bool TryAttachImportedDriver(Transform root)
        {
            const string prefabPath="Assets/Prefabs/Characters/DrivingDriver.prefab";
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if(prefab==null)return false;
            var anchor=new GameObject("DriverAnchor").transform;
            anchor.SetParent(root,false);
            anchor.localPosition=new Vector3(0,0,-.35f);
            anchor.localRotation=Quaternion.identity;
            anchor.localScale=Vector3.one*1.85f;
            var instance=PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if(instance==null){Object.DestroyImmediate(anchor.gameObject);return false;}
            instance.name="DrivingDriver";
            instance.transform.SetParent(anchor,false);
            instance.transform.localPosition=Vector3.zero;
            instance.transform.localRotation=Quaternion.identity;
            instance.transform.localScale=Vector3.one;
            var animator=instance.GetComponentInChildren<Animator>(true);
            if(animator!=null)animator.applyRootMotion=false;
            return true;
        }

        public static void BuildBuilding(Vector3 groundPosition,float yaw,Material facade,Material glass,int variant)
        {
            var root=new GameObject("Harbor Building").transform;root.SetPositionAndRotation(groundPosition,Quaternion.Euler(0,yaw,0));
            float width=9+variant*1.3f,depth=10,height=12+variant*4;
            var trim=Material("FacadeTrim",new Color(.9f,.86f,.75f),.2f);
            Part(root,PrimitiveType.Cube,"MainVolume",new Vector3(0,height/2,0),new Vector3(width,height,depth),facade);
            for(int floor=0;floor<height/3;floor++)
            {
                Part(root,PrimitiveType.Cube,"FloorCornice",new Vector3(0,floor*3+.2f,0),new Vector3(width+.3f,.15f,depth+.3f),trim);
                for(int side=-1;side<=1;side+=2)for(int col=-1;col<=1;col++)
                {
                    Vector3 pos=new Vector3(col*width*.28f,1.6f+floor*3,side*(depth/2+.025f));
                    Part(root,PrimitiveType.Cube,"WindowFrame",pos,new Vector3(2.15f,1.85f,.13f),trim);
                    Part(root,PrimitiveType.Cube,"Glazing",pos+Vector3.forward*(side*.075f),new Vector3(1.88f,1.58f,.04f),glass);
                    Part(root,PrimitiveType.Cube,"WindowMullion",pos+Vector3.forward*(side*.105f),new Vector3(.06f,1.65f,.025f),trim);
                    var sidePos=new Vector3(side*(width/2+.03f),1.6f+floor*3,col*2.8f);
                    Part(root,PrimitiveType.Cube,"SideWindow",sidePos,new Vector3(.075f,1.7f,1.7f),glass);
                }
            }
            Part(root,PrimitiveType.Cube,"RoofParapet",new Vector3(0,height+.22f,0),new Vector3(width+.6f,.45f,depth+.6f),trim);
            Part(root,PrimitiveType.Cube,"Rooftop",new Vector3(0,height+.5f,0),new Vector3(width*.55f,.6f,depth*.55f),glass);
            for(int side=-1;side<=1;side+=2)
            {
                var awning=Part(root,PrimitiveType.Cube,"ShopAwning",new Vector3(0,2.85f,side*5.6f),new Vector3(width*.85f,.16f,1.5f),facade);awning.localRotation=Quaternion.Euler(side*12,0,0);
                Part(root,PrimitiveType.Cube,"ShopDoor",new Vector3(0,1.1f,side*5.11f),new Vector3(1.45f,2.2f,.08f),glass);
            }
            CombineScenery(root,"BuildingV2_"+variant);
        }

        public static void BuildPalm(Vector3 groundPosition,Material green)
        {
            var root=new GameObject("Original Palm").transform;root.position=groundPosition;
            var bark=Material("PalmBark",new Color(.47f,.34f,.2f),.1f);
            Vector3 top=new Vector3(.6f,6.2f,.1f);
            for(int i=0;i<10;i++)
            {float t=i/10f,u=(i+1)/10f;Tube(root,"TrunkSegment",new Vector3(t*t*.6f,t*6.2f,0),new Vector3(u*u*.6f,u*6.2f,0),.18f-t*.07f,bark);}
            for(int leaf=0;leaf<9;leaf++)
            {
                float a=leaf*Mathf.PI*2/9;Vector3 direction=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)),side=Vector3.Cross(Vector3.up,direction);
                for(int part=0;part<6;part++)
                {
                    float t=part/6f,u=(part+1)/6f;Vector3 p=top+direction*(t*3.2f)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*.8f-t*.7f),q=top+direction*(u*3.2f)+Vector3.up*(Mathf.Sin(u*Mathf.PI)*.8f-u*.7f);
                    Tube(root,"LeafStem",p,q,.025f,green);
                    for(int s=-1;s<=1;s+=2)
                    {
                        Vector3 tip=p+side*(s*(.6f*(1-t)+.12f))+direction*.5f-Vector3.up*.16f;
                        var mesh=new Mesh();mesh.vertices=new[]{p,q,tip};mesh.triangles=new[]{0,1,2,2,1,0};mesh.normals=new[]{Vector3.up,Vector3.up,Vector3.up};
                        string key="PalmLeafV2_"+leaf+"_"+part+"_"+s;
                        Mesh saved=SaveMesh(mesh,key);
                        var frond=new GameObject("PalmLeaf");frond.transform.SetParent(root,false);frond.AddComponent<MeshFilter>().sharedMesh=saved;frond.AddComponent<MeshRenderer>().sharedMaterial=green;
                    }
                }
            }
            CombineScenery(root,"PalmV2");
        }
        private static void CombineScenery(Transform root,string key)
        {
            var groups=new Dictionary<Material,List<CombineInstance>>();
            var oldChildren=new List<GameObject>();foreach(Transform child in root)oldChildren.Add(child.gameObject);
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                var material=filter.GetComponent<Renderer>().sharedMaterial;
                if(!groups.ContainsKey(material))groups[material]=new List<CombineInstance>();
                groups[material].Add(new CombineInstance{mesh=filter.sharedMesh,transform=root.worldToLocalMatrix*filter.transform.localToWorldMatrix});
            }
            foreach(var group in groups)
            {
                var mesh=new Mesh();mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.CombineMeshes(group.Value.ToArray());
                mesh=SaveMesh(mesh,key+"_"+group.Key.name);
                var go=new GameObject("Combined "+group.Key.name);go.transform.SetParent(root,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=group.Key;go.isStatic=true;
            }
            foreach(var child in oldChildren)Object.DestroyImmediate(child);
        }
        private static void Hull(Transform root,string name,Vector3 pos,Vector3 scale,Material mat)
        {
            string key="Hull_"+name;string path="Assets/Art/Generated/"+key+".asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh==null)
            {
                Vector2[] polygon={new Vector2(-.36f,-.5f),new Vector2(.36f,-.5f),new Vector2(.5f,-.34f),new Vector2(.5f,.28f),new Vector2(.34f,.5f),new Vector2(-.34f,.5f),new Vector2(-.5f,.28f),new Vector2(-.5f,-.34f)};
                var v=new List<Vector3>();var t=new List<int>();float[] widths={.8f,1,.84f};float[] heights={-.5f,-.22f,.5f};
                for(int ring=0;ring<3;ring++)foreach(var point in polygon)v.Add(new Vector3(point.x*widths[ring],heights[ring],point.y*widths[ring]));
                for(int ring=0;ring<2;ring++)for(int i=0;i<8;i++){int a=ring*8+i,b=ring*8+(i+1)%8,c=a+8,d=b+8;t.AddRange(new[]{a,c,b,b,c,d});}
                for(int i=1;i<7;i++){t.AddRange(new[]{16,16+i+1,16+i});t.AddRange(new[]{0,i,i+1});}
                mesh=new Mesh{name=key};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();mesh=SaveMesh(mesh,key);
            }
            var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.localPosition=pos;go.transform.localScale=scale;go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=mat;
        }
        private static Mesh SaveMesh(Mesh mesh,string name)
        {Directory.CreateDirectory("Assets/Art/Generated");string path="Assets/Art/Generated/"+name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved!=null){Object.DestroyImmediate(mesh);return saved;}AssetDatabase.CreateAsset(mesh,path);return mesh;}
        private static Transform Part(Transform root,PrimitiveType type,string name,Vector3 pos,Vector3 scale,Material material)
        {var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(root,false);go.transform.localPosition=pos;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;Object.DestroyImmediate(go.GetComponent<Collider>());return go.transform;}
        private static void Tube(Transform root,string name,Vector3 a,Vector3 b,float radius,Material material)
        {Vector3 d=b-a;var tube=Part(root,PrimitiveType.Cylinder,name,(a+b)*.5f,new Vector3(radius*2,d.magnitude*.5f,radius*2),material);tube.localRotation=Quaternion.FromToRotation(Vector3.up,d.normalized);}
        private static Material Material(string name,Color color,float smooth,float metal=0)
        {Directory.CreateDirectory("Assets/Materials/Generated");string path="Assets/Materials/Generated/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat==null){mat=new Material(Shader.Find("Standard"));mat.color=color;mat.SetFloat("_Glossiness",smooth);mat.SetFloat("_Metallic",metal);AssetDatabase.CreateAsset(mat,path);}return mat;}
    }
}
