using System.IO;
using OriginalKartRacer.CameraSystem;
using OriginalKartRacer.Effects;
using OriginalKartRacer.Race;
using OriginalKartRacer.Replay;
using OriginalKartRacer.UI;
using OriginalKartRacer.Vehicle;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace OriginalKartRacer.EditorTools
{
    /// <summary>可重复生成的原创海港赛道。生成资产单独存放，保留旧场景供比较。</summary>
    public static class PlayableDemoSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/Race_Coast_02.unity";
        private const string Generated = "Assets/Materials/Generated/";
        private const int Segments = 240;
        private static Vector3[] points;

        [MenuItem("Neon Harbor/Build Coast 02 Scene")]
        public static void BuildDemo()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Generated);
            Directory.CreateDirectory("Assets/Art/Generated");
            Directory.CreateDirectory("Assets/Scenes");
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var road = Mat("Asphalt", new Color(.24f,.27f,.29f), .12f);
            var white = Mat("Ivory", new Color(.96f,.95f,.89f));
            var blue = Mat("OceanBlue", new Color(.04f,.43f,.73f));
            var dark = Mat("Rubber", new Color(.038f,.044f,.054f), .12f);
            var metal = Mat("Rail", new Color(.55f,.63f,.68f), .65f, .6f);
            var glass = Mat("Glass", new Color(.11f,.28f,.36f), .82f, .35f);
            var grass = Mat("Lawn", new Color(.34f,.53f,.27f), .05f);
            var green = Mat("PalmGreen", new Color(.16f,.43f,.22f), .18f);
            var orange = Mat("SunsetPaint", new Color(.97f,.22f,.075f), .72f, .32f);
            var skin = Mat("Skin", new Color(.85f,.55f,.35f), .18f);
            var concrete = Mat("Paving", new Color(.71f,.69f,.61f), .15f);
            road.mainTexture = NoiseTexture("AsphaltGrain", new Color(.82f,.82f,.82f), .22f);
            grass.mainTexture = NoiseTexture("GrassGrain", Color.white, .16f);
            var terrain = Box("Parkland", new Vector3(0,-.55f,0), new Vector3(250,1,250), grass, true);
            SetSurface(terrain, "Grass", .55f, .48f, new Color(.48f,.42f,.24f,.55f));
            Box("Sea", new Vector3(230,-.18f,0), new Vector3(230,.12f,600), Mat("Sea",new Color(.08f,.54f,.67f),.8f), false);
            points = MakePath();
            var track = Strip("Circuit_Asphalt", -6.5f, 6.5f, .02f, road, true);
            SetSurface(track, "Asphalt", 1, 1, new Color(.07f,.065f,.06f,.7f));
            Strip("OutsideWalk",6.9f,9.5f,.06f,concrete,false);
            Strip("InsideWalk",-9.5f,-6.9f,.06f,concrete,false);
            for(int i=0;i<Segments;i++)
            {
                Vector3 p=points[i], q=points[(i+1)%Segments], d=q-p, right=Vector3.Cross(Vector3.up,d.normalized);
                Quaternion rotation=Quaternion.LookRotation(d);
                foreach(int side in new[]{-1,1})
                {
                    var curb=Box("StripedCurb",(p+q)*.5f+right*(side*6.65f)+Vector3.up*.04f,
                        new Vector3(.42f,.08f,d.magnitude+.03f),(i/3)%2==0?white:blue,false);
                    curb.transform.rotation=rotation;
                    var rail=Box("SafetyWall",(p+q)*.5f+right*(side*9.1f)+Vector3.up*.48f,
                        new Vector3(.3f,.96f,d.magnitude+.1f),white,true);
                    rail.transform.rotation=rotation;
                    var top=Box("BlueRail",rail.transform.position+Vector3.up*.34f,new Vector3(.34f,.2f,d.magnitude+.1f),blue,false);
                    top.transform.rotation=rotation;
                    if(i%12==0)
                    {
                        Text("RailBrand", "NEON  HARBOR",p+right*(side*8.92f)+Vector3.up*.54f,
                            Quaternion.LookRotation(right*side),.16f,blue.color);
                    }
                }
                if(i%4==0)
                {
                    var dash=Box("CenterDash",(p+q)*.5f+Vector3.up*.027f,new Vector3(.12f,.009f,d.magnitude*1.7f),white,false);
                    dash.transform.rotation=rotation;
                }
                if(i%20==0)
                {
                    OriginalRaceArt.BuildPalm(p+right*11,green);
                    StreetLight(p-right*10,rotation,metal,white);
                }
                if(i%40==20) Arrow(p+right*7.8f+Vector3.up*1.6f,rotation,blue,white);
            }
            // 楼体全部为真正的 3D 几何，窗户和檐口随同一根节点旋转。
            var facades=new[]{Mat("Coral",new Color(.85f,.43f,.31f)),Mat("Sand",new Color(.85f,.74f,.55f)),Mat("Mint",new Color(.40f,.65f,.59f)),Mat("Pearl",new Color(.81f,.84f,.81f))};
            for(int i=0;i<24;i++)
            {
                float a=i*Mathf.PI*2/24;
                Vector3 p=new Vector3(Mathf.Cos(a)*84,0,Mathf.Sin(a)*86);
                OriginalRaceArt.BuildBuilding(p,Mathf.Atan2(-p.x,-p.z)*Mathf.Rad2Deg,facades[i%4],glass,i%4);
                if(i%2==0) OriginalRaceArt.BuildPalm(p*.79f,green);
            }
            // 中央花园、雕塑和观众看台丰富近景，不挡住行驶路线。
            Box("GardenPlaza",new Vector3(0,0,5),new Vector3(31,.12f,25),concrete,false);
            for(int i=0;i<8;i++) OriginalRaceArt.BuildPalm(new Vector3(-12+i*3.4f,0,17),green);
            for(int row=0;row<4;row++)
                Box("Grandstand",new Vector3(-13,.5f+row*.6f,-16-row*1.4f),new Vector3(20,.6f,1.5f),row%2==0?blue:white,false);
            var sculpture=GameObject.CreatePrimitive(PrimitiveType.Capsule);
            sculpture.name="HarborSculpture"; sculpture.transform.position=new Vector3(0,3,5); sculpture.transform.localScale=new Vector3(1.4f,3,1.4f);
            sculpture.transform.rotation=Quaternion.Euler(0,0,24); sculpture.GetComponent<Renderer>().sharedMaterial=metal;

            Vector3 forward=(points[1]-points[0]).normalized;
            Quaternion startRotation=Quaternion.LookRotation(forward);
            var startRoot=new GameObject("StartFinish_Plaza").transform;
            startRoot.SetPositionAndRotation(points[0],startRotation);
            for(int x=0;x<10;x++) for(int z=0;z<4;z++)
                LocalBox(startRoot,"StartCheck",new Vector3(-5.85f+x*1.3f,.034f,-2+z*1.3f),new Vector3(1.3f,.015f,1.3f),(x+z)%2==0?blue:white);
            LocalBox(startRoot,"GateLeft",new Vector3(-6,3.3f,0),new Vector3(.65f,6.6f,.85f),blue);
            LocalBox(startRoot,"GateRight",new Vector3(6,3.3f,0),new Vector3(.65f,6.6f,.85f),blue);
            LocalBox(startRoot,"GateTop",new Vector3(0,6.3f,0),new Vector3(13,.95f,.85f),blue);
            Text("GateLogo","NEON  HARBOR",startRoot.TransformPoint(new Vector3(0,6.3f,-.44f)),startRotation,.46f,Color.white);
            Text("GateReverseLogo","NEON  HARBOR",startRoot.TransformPoint(new Vector3(0,6.3f,.44f)),startRotation*Quaternion.Euler(0,180,0),.46f,Color.white);

            var systems=new GameObject("RaceSystems");
            var clock=systems.AddComponent<RaceClock>();
            var manager=systems.AddComponent<RaceManager>();
            var path=new GameObject("Editable Racing Path").AddComponent<RacingPath>();
            path.SetWorldPoints(points);
            var board=systems.AddComponent<RacePositionBoard>(); Set(board,"path",path);
            var player=CreateVehicle("PlayerKart",points[0]+forward*4+Vector3.up*.5f,startRotation,orange,blue,dark,skin,glass);
            Set(manager,"playerKart",player); Set(manager,"clock",clock); SetInt(manager,"totalLaps",2);
            board.Register(player);
            var opponentPaint=new[]{Mat("RivalMint",new Color(.2f,.72f,.52f),.7f,.3f),Mat("RivalYellow",new Color(1,.72f,.06f),.7f,.3f),Mat("RivalViolet",new Color(.46f,.3f,.78f),.7f,.3f),Mat("RivalWhite",new Color(.94f,.94f,.9f),.7f,.3f),Mat("RivalRed",new Color(.75f,.08f,.1f),.7f,.3f)};
            for(int i=0;i<5;i++)
            {
                int index=Segments-2-i*2;
                Vector3 dir=(points[(index+1)%Segments]-points[index]).normalized;
                var aiKart=CreateVehicle("Rival_"+(i+1),points[index]+Vector3.up*.5f+Vector3.Cross(Vector3.up,dir)*(i%2==0?1.8f:-1.8f),Quaternion.LookRotation(dir),opponentPaint[i],white,dark,skin,glass);
                var ai=aiKart.gameObject.AddComponent<RaceAIDriver>(); Set(ai,"path",path); Set(ai,"raceClock",clock); board.Register(aiKart);
            }
            var sequence=systems.AddComponent<CheckpointSequence>(); Set(sequence,"raceManager",manager); SetInt(sequence,"checkpointCount",4);
            int[] checkpointIndices={60,120,180,226};
            for(int i=0;i<4;i++)
            {
                int index=checkpointIndices[i];
                var trigger=Trigger("Checkpoint_"+i,points[index],Quaternion.LookRotation(points[(index+1)%Segments]-points[index]));
                var cp=trigger.AddComponent<CheckpointTrigger>(); Set(cp,"sequence",sequence); SetInt(cp,"checkpointIndex",i);
            }
            var lap=Trigger("FinishLine",points[0],startRotation).AddComponent<LapLineTrigger>(); Set(lap,"raceManager",manager);
            var recorder=player.gameObject.AddComponent<RaceReplayRecorder>(); Set(recorder,"raceClock",clock); Set(recorder,"raceManager",manager); Set(recorder,"nitro",player.GetComponent<DriftNitroSystem>());
            var telemetry=player.gameObject.AddComponent<ReplayTelemetry>();
            player.gameObject.AddComponent<RaceReplayPlayer>();
            systems.AddComponent<RaceSessionControls>();

            var cam=new GameObject("Main Camera").AddComponent<Camera>(); cam.tag="MainCamera"; cam.fieldOfView=62; cam.farClipPlane=500;
            cam.gameObject.AddComponent<AudioListener>();
            cam.transform.position=player.transform.position-player.transform.forward*6.6f+Vector3.up*3;
            cam.transform.LookAt(player.transform.position+player.transform.forward*4+Vector3.up*.8f);
            var follow=cam.gameObject.AddComponent<ThirdPersonKartCamera>(); Set(follow,"target",player.transform); Set(follow,"targetBody",player.GetComponent<Rigidbody>());
            var hud=new GameObject("Race HUD").AddComponent<DemoRuntimeHUD>();
            hud.RaceManager=manager; hud.RaceClock=clock; hud.Kart=player; hud.Nitro=player.GetComponent<DriftNitroSystem>(); hud.PositionBoard=board; hud.ReplayTelemetry=telemetry;
            Lighting();
            PlayerSettings.colorSpace=ColorSpace.Linear;
            PlayerSettings.runInBackground=true;
            PlayerSettings.defaultScreenWidth=1600; PlayerSettings.defaultScreenHeight=900;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            PlayerSettings.productName="Neon Harbor - Coast Circuit";
            PlayerSettings.companyName="Original Race Studio";
            QualitySettings.antiAliasing=4; QualitySettings.shadowDistance=100; QualitySettings.shadows=ShadowQuality.All; QualitySettings.shadowResolution=ShadowResolution.High;
            EditorSceneManager.SaveScene(scene,ScenePath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            AssetDatabase.SaveAssets();
            Selection.activeGameObject=player.gameObject;
            Debug.Log("COAST_SCENE_READY "+ScenePath);
        }

        [MenuItem("Neon Harbor/Build Windows Game")]
        public static void BuildWindows()
        {
            BuildDemo();
            Directory.CreateDirectory("Builds/Windows");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Builds/Windows/NeonHarbor.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
            if(report.summary.result!=BuildResult.Succeeded) throw new System.Exception("Build failed: "+report.summary.result);
            Directory.CreateDirectory("Builds/Windows/Licenses");
            File.Copy("ThirdPartyNotices/RVP-MIT.txt","Builds/Windows/Licenses/RVP-MIT.txt",true);
            Debug.Log("COAST_BUILD_OK");
        }

        public static void BuildPreview()
        {
            BuildDemo();
            var camera=Camera.main;
            var rt=new RenderTexture(1600,900,24);
            camera.targetTexture=rt; camera.Render(); RenderTexture previous=RenderTexture.active; RenderTexture.active=rt;
            var screenshot=new Texture2D(1600,900,TextureFormat.RGB24,false);
            screenshot.ReadPixels(new Rect(0,0,1600,900),0,0); screenshot.Apply();
            Directory.CreateDirectory("Screenshots"); File.WriteAllBytes("Screenshots/Coast02-Scene.png",screenshot.EncodeToPNG());
            camera.targetTexture=null; RenderTexture.active=previous; Object.DestroyImmediate(rt); Object.DestroyImmediate(screenshot);
            Debug.Log("COAST_PREVIEW_OK");
        }

        private static KartController CreateVehicle(string name,Vector3 position,Quaternion rotation,Material paint,Material accent,Material rubber,Material skin,Material glass)
        {
            var go=new GameObject(name); go.transform.SetPositionAndRotation(position,rotation);
            var body=go.AddComponent<Rigidbody>(); body.mass=180; body.drag=.05f; body.angularDrag=3; body.constraints=RigidbodyConstraints.FreezeRotationX|RigidbodyConstraints.FreezeRotationZ;
            var collider=go.AddComponent<BoxCollider>(); collider.size=new Vector3(1.75f,.7f,2.6f);
            var controller=go.AddComponent<KartController>(); var nitro=go.AddComponent<DriftNitroSystem>();
            controller.EnsureWheelLayout();
            OriginalRaceArt.BuildKart(go.transform,paint,accent,rubber,skin,glass);
            go.AddComponent<VehicleLightController>(); go.AddComponent<RoadSurfaceDetector>();
            go.AddComponent<KartVisualMotion>();
            go.AddComponent<KartAudio>();
            BuildEffects(go,nitro);
            return controller;
        }

        private static void BuildEffects(GameObject vehicle,DriftNitroSystem nitro)
        {
            var effects=vehicle.AddComponent<KartEffectsController>(); Set(effects,"driftNitro",nitro);
            var trails=new TrailRenderer[2]; var smoke=new ParticleSystem[2]; var flames=new ParticleSystem[2];
            var skidMat=Mat("Skid",new Color(.04f,.038f,.035f),0);
            skidMat.shader=Shader.Find("Sprites/Default");skidMat.color=Color.white;
            var particleMat=AssetDatabase.LoadAssetAtPath<Material>(Generated+"Particles.mat");
            if(particleMat==null){particleMat=new Material(Shader.Find("Particles/Standard Unlit"));AssetDatabase.CreateAsset(particleMat,Generated+"Particles.mat");}
            particleMat.SetFloat("_Mode",2); particleMat.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha); particleMat.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha); particleMat.SetFloat("_ZWrite",0); particleMat.renderQueue=3000;
            particleMat.EnableKeyword("_ALPHABLEND_ON");
            for(int i=0;i<2;i++)
            {
                float side=i==0?-1:1;
                var trail=new GameObject("Tire mark").AddComponent<TrailRenderer>(); trail.transform.SetParent(vehicle.transform,false); trail.transform.localPosition=new Vector3(side*.91f,-.325f,-.86f);
                trail.time=8; trail.startWidth=.17f; trail.endWidth=.12f; trail.minVertexDistance=.12f; trail.sharedMaterial=skidMat; trail.emitting=false; trail.shadowCastingMode=ShadowCastingMode.Off; trails[i]=trail;
                smoke[i]=Particles(vehicle.transform,"DriftSmoke",new Vector3(side*.9f,-.2f,-.8f),new Color(.76f,.79f,.8f,.24f),particleMat,.7f,.4f,14);
                flames[i]=Particles(vehicle.transform,"NitroJet",new Vector3(side*.52f,.08f,-1.37f),new Color(.12f,.64f,1f,.8f),particleMat,.18f,.24f,80);
                var main=flames[i].main; main.startSpeed=7;
            }
            SetArray(effects,"skidTrails",trails); SetArray(effects,"driftSmoke",smoke); SetArray(effects,"boostFlames",flames); SetArray(effects,"exhaust",new Object[0]);
        }

        private static ParticleSystem Particles(Transform parent,string name,Vector3 local,Color color,Material material,float lifetime,float size,float rate)
        {
            var go=new GameObject(name); go.transform.SetParent(parent,false); go.transform.localPosition=local; go.transform.localRotation=Quaternion.Euler(0,180,0);
            var ps=go.AddComponent<ParticleSystem>(); ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main; main.playOnAwake=false; main.startLifetime=lifetime; main.startSize=size; main.startColor=color; main.startSpeed=.5f; main.simulationSpace=ParticleSystemSimulationSpace.World; main.maxParticles=200;
            var emission=ps.emission; emission.rateOverTime=rate;
            var shape=ps.shape; shape.shapeType=ParticleSystemShapeType.Cone; shape.angle=12; shape.radius=.035f;
            var sizeLife=ps.sizeOverLifetime; sizeLife.enabled=true; sizeLife.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,1,1,0));
            var renderer=ps.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial=material; renderer.shadowCastingMode=ShadowCastingMode.Off;
            return ps;
        }

        private static Vector3[] MakePath()
        {
            Vector3[] anchors={new Vector3(0,0,-55),new Vector3(37,0,-53),new Vector3(57,0,-24),new Vector3(39,0,4),new Vector3(56,0,38),new Vector3(25,0,60),new Vector3(-7,0,40),new Vector3(-32,0,60),new Vector3(-55,0,31),new Vector3(-38,0,0),new Vector3(-54,0,-32),new Vector3(-29,0,-56)};
            var result=new Vector3[Segments]; int n=anchors.Length;
            for(int i=0;i<Segments;i++)
            {
                float t=(float)i/Segments*n; int k=Mathf.FloorToInt(t); t-=k;
                Vector3 a=anchors[(k+n-1)%n],b=anchors[k],c=anchors[(k+1)%n],d=anchors[(k+2)%n];
                result[i]=.5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t);
            }
            return result;
        }

        private static GameObject Strip(string name,float left,float right,float height,Material mat,bool collision)
        {
            var vertices=new Vector3[(Segments+1)*2]; var uv=new Vector2[vertices.Length]; var triangles=new int[Segments*6]; float distance=0;
            for(int i=0;i<=Segments;i++)
            {
                int k=i%Segments; Vector3 tangent=(points[(k+1)%Segments]-points[(k+Segments-1)%Segments]).normalized; Vector3 r=Vector3.Cross(Vector3.up,tangent);
                if(i>0) distance+=Vector3.Distance(points[k],points[(k+Segments-1)%Segments]);
                vertices[i*2]=points[k]+r*left+Vector3.up*height; vertices[i*2+1]=points[k]+r*right+Vector3.up*height;
                uv[i*2]=new Vector2(0,distance*.5f); uv[i*2+1]=new Vector2((right-left)*.5f,distance*.5f);
                if(i<Segments){int v=i*2,j=i*6; triangles[j]=v;triangles[j+1]=v+2;triangles[j+2]=v+1;triangles[j+3]=v+1;triangles[j+4]=v+2;triangles[j+5]=v+3;}
            }
            var mesh=new Mesh{name=name}; mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();
            string asset="Assets/Art/Generated/"+name+".asset"; var existing=AssetDatabase.LoadAssetAtPath<Mesh>(asset);
            if(existing!=null){EditorUtility.CopySerialized(mesh,existing); Object.DestroyImmediate(mesh);mesh=existing;}else AssetDatabase.CreateAsset(mesh,asset);
            var go=new GameObject(name);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=mat;
            if(collision) go.AddComponent<MeshCollider>().sharedMesh=mesh;
            return go;
        }

        private static void Lighting()
        {
            var sun=new GameObject("Afternoon Sun").AddComponent<Light>();sun.type=LightType.Directional;sun.color=new Color(1,.94f,.83f);sun.intensity=1.15f;sun.shadows=LightShadows.Soft;sun.shadowStrength=.65f;sun.transform.rotation=Quaternion.Euler(48,-32,0);
            RenderSettings.sun=sun; RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.62f,.76f,.88f);RenderSettings.ambientEquatorColor=new Color(.49f,.57f,.61f);RenderSettings.ambientGroundColor=new Color(.37f,.39f,.37f);
            var sky=AssetDatabase.LoadAssetAtPath<Material>(Generated+"Sky.mat");if(sky==null){sky=new Material(Shader.Find("Skybox/Procedural"));AssetDatabase.CreateAsset(sky,Generated+"Sky.mat");}
            sky.SetFloat("_SunSize",.025f);sky.SetFloat("_AtmosphereThickness",.8f);sky.SetColor("_SkyTint",new Color(.5f,.58f,.65f));sky.SetColor("_GroundColor",new Color(.56f,.67f,.67f));sky.SetFloat("_Exposure",1.15f);RenderSettings.skybox=sky;
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=130;RenderSettings.fogEndDistance=360;RenderSettings.fogColor=new Color(.65f,.8f,.86f);
        }

        private static Material Mat(string name,Color color,float smooth=.3f,float metallic=0)
        {
            var mat=AssetDatabase.LoadAssetAtPath<Material>(Generated+name+".mat");if(mat==null){mat=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(mat,Generated+name+".mat");}
            mat.color=color;mat.SetFloat("_Glossiness",smooth);mat.SetFloat("_Metallic",metallic);EditorUtility.SetDirty(mat);return mat;
        }
        private static Texture2D NoiseTexture(string name,Color color,float amount)
        {
            string path="Assets/Art/Generated/"+name+".asset";var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(texture!=null)return texture;
            texture=new Texture2D(128,128,TextureFormat.RGB24,true);texture.name=name;texture.wrapMode=TextureWrapMode.Repeat;var pixels=new Color[128*128];var random=new System.Random(7);
            for(int i=0;i<pixels.Length;i++)pixels[i]=color*(1-amount*(float)random.NextDouble());texture.SetPixels(pixels);texture.Apply();AssetDatabase.CreateAsset(texture,path);return texture;
        }
        private static GameObject Box(string name,Vector3 pos,Vector3 scale,Material mat,bool collider)
        {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.position=pos;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=mat;if(!collider)Object.DestroyImmediate(go.GetComponent<Collider>());go.isStatic=true;return go;}
        private static void LocalBox(Transform parent,string name,Vector3 local,Vector3 scale,Material mat)
        {var go=Box(name,Vector3.zero,scale,mat,false);go.transform.SetParent(parent,false);go.transform.localPosition=local;}
        private static void Text(string name,string value,Vector3 position,Quaternion rotation,float size,Color color)
        {
            var text=new GameObject(name).AddComponent<TextMesh>();text.text=value;text.fontSize=64;text.characterSize=size;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=color;text.transform.SetPositionAndRotation(position,rotation);
            var mat=AssetDatabase.LoadAssetAtPath<Material>(Generated+"WorldText.mat");
            if(mat==null){mat=new Material(Shader.Find("NeonHarbor/WorldText"));AssetDatabase.CreateAsset(mat,Generated+"WorldText.mat");}
            mat.mainTexture=text.font.material.mainTexture;text.GetComponent<Renderer>().sharedMaterial=mat;
            text.gameObject.AddComponent<OriginalKartRacer.Track.WorldLabel>();
        }
        private static GameObject Trigger(string name,Vector3 position,Quaternion rotation)
        {var go=new GameObject(name);go.transform.SetPositionAndRotation(position+Vector3.up*1.5f,rotation);var c=go.AddComponent<BoxCollider>();c.isTrigger=true;c.size=new Vector3(18,4,1);return go;}
        private static void StreetLight(Vector3 position,Quaternion rotation,Material pole,Material white)
        {var root=new GameObject("Street lamp").transform;root.SetPositionAndRotation(position,rotation);LocalBox(root,"Column",new Vector3(0,3.3f,0),new Vector3(.12f,6.6f,.12f),pole);LocalBox(root,"Arm",new Vector3(1,6.5f,0),new Vector3(2,.12f,.12f),pole);LocalBox(root,"Lamp",new Vector3(1.9f,6.4f,0),new Vector3(.7f,.16f,.35f),white);}
        private static void Arrow(Vector3 position,Quaternion rotation,Material blue,Material white)
        {var sign=Box("Corner guide",position,new Vector3(1.9f,.8f,.1f),blue,false);sign.transform.rotation=rotation;Text("Direction","»",position-rotation*Vector3.forward*.06f,rotation,.8f,white.color);}
        private static void SetSurface(GameObject go,string name,float grip,float acceleration,Color skid)
        {string asset=Generated+name+"Surface.asset";var profile=AssetDatabase.LoadAssetAtPath<RoadSurfaceProfile>(asset);if(profile==null){profile=ScriptableObject.CreateInstance<RoadSurfaceProfile>();AssetDatabase.CreateAsset(profile,asset);}profile.lateralGripMultiplier=grip;profile.accelerationMultiplier=acceleration;profile.skidColor=skid;EditorUtility.SetDirty(profile);var volume=go.AddComponent<RoadSurfaceVolume>();Set(volume,"profile",profile);}
        private static void Set(Object target,string name,Object value){var s=new SerializedObject(target);s.FindProperty(name).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
        private static void SetInt(Object target,string name,int value){var s=new SerializedObject(target);s.FindProperty(name).intValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
        private static void SetArray(Object target,string name,Object[] values){var s=new SerializedObject(target);var p=s.FindProperty(name);p.arraySize=values.Length;for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];s.ApplyModifiedPropertiesWithoutUndo();}
    }
}
