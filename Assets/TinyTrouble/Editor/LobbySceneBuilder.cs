using System;
using System.Collections.Generic;
using System.Linq;
using TinyTrouble;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace TinyTrouble.Editor
{
    /// <summary>
    /// Создаёт редактируемое лобби по команде в редакторе Unity.
    /// </summary>
    /// <remarks>
    /// Во время игры этот класс не строит меню заново: открывается уже сохранённая сцена.
    /// </remarks>
    public static class LobbySceneBuilder
    {
        /// <summary>
        /// Общая папка материалов, моделей и других файлов лобби.
        /// </summary>
        const string Root = "Assets/TinyTrouble";
        /// <summary>
        /// Обычный и жирный шрифты с русскими буквами.
        /// </summary>
        static Font regular, bold;
        /// <summary>
        /// Основные цвета интерфейса: тёмный, приглушённый, бирюзовый и жёлтый.
        /// </summary>
        static Color Ink = Hex("#0C1831"), Muted = Hex("#A7BAD7"), Cyan = Hex("#66E5FF"), Yellow = Hex("#FFD84A");
        /// <summary>
        /// Переводит текстовый код цвета в цвет Unity.
        /// </summary>
        /// <param name="html">Код вида #FFD84A.</param>
        /// <returns>Цвет для материала или интерфейса.</returns>
        static Color Hex(string html) { ColorUtility.TryParseHtmlString(html, out var c); return c; }

        /// <summary>
        /// Создаёт и сохраняет лобби по команде Tiny Trouble/Build Lobby.
        /// </summary>
        /// <remarks>
        /// Повторная команда пересобирает сцену и шаблон героя и может заменить ручные правки. Перед созданием сцены предлагаем сохранить изменения.
        /// </remarks>
        [MenuItem("Tiny Trouble/Build Lobby")]
        public static void Build()
        {
            // Создаём сцену только вне режима игры, сначала предлагаем сохранить открытые сцены.
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before building Lobby.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            foreach (var folder in new[] { "/Materials", "/Prefabs", "/Meshes" })
                if (!AssetDatabase.IsValidFolder(Root + folder)) AssetDatabase.CreateFolder(Root, folder.Substring(1));
            regular = AssetDatabase.LoadAssetAtPath<Font>(Root + "/UI/Fonts/DejaVuSans.ttf");
            bold = AssetDatabase.LoadAssetAtPath<Font>(Root + "/UI/Fonts/DejaVuSans-Bold.ttf");
            if (!regular || !bold) throw new InvalidOperationException("Import the Cyrillic fonts before building Lobby.");
            // Новая сцена получает мягкое общее освещение.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Hex("#6683A5");
            RenderSettings.ambientIntensity = .8f;
            RenderSettings.fog = false;
            // Камера смотрит на героя; AudioListener — компонент, через который игрок слышит звук.
            var cameraGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraGo.tag = "MainCamera";
            cameraGo.transform.position = new Vector3(0, 1.75f, -6.8f);
            cameraGo.transform.LookAt(new Vector3(0, 1.32f, 0));
            var camera = cameraGo.GetComponent<Camera>();
            // Передаём нажатия мыши объёмной области героя через ту же систему событий, что и меню.
            cameraGo.AddComponent<PhysicsRaycaster>();
            camera.fieldOfView = 37;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Hex("#0C1934");
            camera.nearClipPlane = .1f;
            // Главный, дополнительный и задний свет помогают видеть объём человечка.
            Light("Key Light", LightType.Directional, new Vector3(45,-28,0), Hex("#FFF2DB"), .9f);
            var fill = Light("Fill Light", LightType.Point, Vector3.zero, Hex("#83CAFF"), 1.1f);
            fill.transform.position = new Vector3(2.5f, 2.8f, -2); fill.range = 8;
            var rimLight = Light("Rim Light", LightType.Point, Vector3.zero, Hex("#BBA9FF"), 1.4f);
            rimLight.transform.position = new Vector3(-1.8f, 2.5f, 1.5f); rimLight.range = 6;
            // Плоскость с нашим правилом рисования (shader) создаёт синий фон.
            var backdropMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Backdrop.mat");
            if (!backdropMaterial) { backdropMaterial = new Material(Shader.Find("TinyTrouble/LobbyBackdrop")); AssetDatabase.CreateAsset(backdropMaterial, Root + "/Materials/Backdrop.mat"); }
            Part("Backdrop", PrimitiveType.Quad, null, new Vector3(0,3,5), new Vector3(30,16,1), backdropMaterial);
            // Основание платформы и два кольца располагаются ниже ног героя.
            var platform = new GameObject("Platform").transform;
            Part("PlatformBase", PrimitiveType.Cylinder, platform, new Vector3(0,-.04f,0), new Vector3(3.25f,.09f,3.25f), Mat("Platform", "#203F60", .55f));
            Part("PlatformTop", PrimitiveType.Cylinder, platform, new Vector3(0,.055f,0), new Vector3(3.05f,.025f,3.05f), Mat("PlatformTop", "#3A5772", .35f));
            Ring("PlatformGlow", platform, 1.5f, 1.59f, .09f, Mat("Glow", "#66E5FF", 0, true));
            Ring("OuterOrbit", platform, 1.83f, 1.84f, .025f, Mat("Orbit", "#285779", 0, true));
            // В сцену помещаем копию сохранённого шаблона героя.
            var heroAsset = MakeCharacter();
            var hero = (GameObject)PrefabUtility.InstantiatePrefab(heroAsset);
            hero.name = "LobbyCharacter"; hero.transform.position = new Vector3(0,.09f,0);
            hero.transform.rotation = Quaternion.Euler(0,-5,0);
            var ui = MakeUI(hero);
            // Сохраняем сцену и порядок запуска: сначала Lobby, затем SampleScene.
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Lobby.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Lobby.unity", true), new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity", true) };
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = ui;
            Debug.Log("Tiny Trouble Lobby built and saved.");
        }
        /// <summary>
        /// Создаёт источник света.
        /// </summary>
        /// <param name="name">Имя объекта в сцене.</param>
        /// <param name="type">Вид света: направленный или точечный.</param>
        /// <param name="rotation">Поворот в градусах.</param>
        /// <param name="color">Цвет света.</param>
        /// <param name="intensity">Яркость.</param>
        /// <returns>Созданный компонент света.</returns>
        static Light Light(string name, LightType type, Vector3 rotation, Color color, float intensity)
        {
            var go = new GameObject(name, typeof(Light)); go.transform.eulerAngles = rotation;
            var light = go.GetComponent<Light>(); light.type = type; light.color = color; light.intensity = intensity;
            light.shadows = type == LightType.Directional ? LightShadows.Soft : LightShadows.None;
            return light;
        }
        /// <summary>
        /// Находит или создаёт материал — файл цвета и блеска поверхности.
        /// </summary>
        /// <param name="name">Имя файла материала.</param>
        /// <param name="hex">Код цвета.</param>
        /// <param name="smoothness">Гладкость от 0 до 1: больше — сильнее блеск.</param>
        /// <param name="emission">Делать ли поверхность светящейся.</param>
        /// <returns>Материал для детали сцены.</returns>
        /// <remarks>
        /// Светящаяся поверхность не является отдельной лампой.
        /// </remarks>
        static Material Mat(string name, string hex, float smoothness = .2f, bool emission = false)
        {
            var path = Root + "/Materials/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) { m = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(m, path); }
            m.color = Hex(hex); m.SetFloat("_Glossiness", smoothness);
            if (emission) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Hex(hex) * .85f); }
            EditorUtility.SetDirty(m); return m;
        }
        /// <summary>
        /// Создаёт деталь из шара, капсулы, цилиндра или плоскости.
        /// </summary>
        /// <param name="name">Имя детали.</param>
        /// <param name="kind">Вид простой формы.</param>
        /// <param name="parent">Родительский объект, внутри которого находится деталь.</param>
        /// <param name="position">Положение относительно родителя.</param>
        /// <param name="scale">Размер по трём осям.</param>
        /// <param name="material">Внешний вид поверхности.</param>
        /// <returns>Созданная деталь.</returns>
        static GameObject Part(string name, PrimitiveType kind, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(kind); go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = scale;
            // Collider отвечает за столкновения. Декоративным деталям меню столкновения не нужны.
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }
        /// <summary>
        /// Строит кольцо из треугольников и сохраняет его геометрию.
        /// </summary>
        /// <param name="name">Имя кольца и файла геометрии.</param>
        /// <param name="parent">Родительский объект платформы.</param>
        /// <param name="inner">Внутренний радиус.</param>
        /// <param name="outer">Внешний радиус.</param>
        /// <param name="y">Высота над центром родителя.</param>
        /// <param name="material">Внешний вид кольца.</param>
        static void Ring(string name, Transform parent, float inner, float outer, float y, Material material)
        {
            // 96 участков дают гладкое кольцо. vertices — точки, triangles — треугольники между ними.
            const int segments = 96;
            var vertices = new Vector3[segments * 2]; var triangles = new int[segments * 6];
            for (int i = 0; i < segments; i++)
            {
                var a = i * Mathf.PI * 2 / segments; var direction = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                vertices[i*2] = direction * inner; vertices[i*2+1] = direction * outer;
                var next = (i+1)%segments;
                int j=i*6; triangles[j]=i*2; triangles[j+1]=next*2; triangles[j+2]=i*2+1;
                triangles[j+3]=i*2+1; triangles[j+4]=next*2; triangles[j+5]=next*2+1;
            }
            // Mesh — геометрия кольца. Сохраняем её, чтобы она осталась после закрытия редактора.
            var path = Root + "/Meshes/" + name + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (!mesh) { mesh = new Mesh(); AssetDatabase.CreateAsset(mesh, path); }
            mesh.Clear(); mesh.name=name; mesh.vertices=vertices; mesh.triangles=triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds(); EditorUtility.SetDirty(mesh);
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(parent,false); go.transform.localPosition=Vector3.up*y;
            go.GetComponent<MeshFilter>().sharedMesh=mesh; go.GetComponent<MeshRenderer>().sharedMaterial=material;
        }
        /// <summary>
        /// Собирает человечка из простых форм и сохраняет prefab — шаблон героя.
        /// </summary>
        /// <returns>Сохранённый шаблон, из которого можно создать новые копии.</returns>
        static GameObject MakeCharacter()
        {
            // Все части будут дочерними объектами героя; материалы кожи и одежды разделены.
            var root = new GameObject("LobbyCharacter"); var t = root.transform;
            var skin = Mat("Skin", "#EFB48D", .3f); var hoodie = Mat("Hoodie", "#FFD84A", .25f);
            var pants = Mat("Trousers", "#325185", .2f); var shoe = Mat("Sneakers", "#172B4F", .35f);
            var cream = Mat("Cream", "#FFF7E2", .25f); var hair = Mat("Hair", "#482B33", .2f);
            var iris = Mat("Eyes", "#183047", .65f); var rosy = Mat("Cheeks", "#E28B7E", .2f);
            // s равен -1 для левой стороны и 1 для правой: один цикл создаёт парные детали.
            for (var s=-1; s<=1; s+=2)
            {
                string side=s<0 ? "Left" : "Right";
                Part(side+"Sole", PrimitiveType.Sphere,t,new Vector3(s*.25f,.07f,-.1f),new Vector3(.43f,.13f,.64f),cream);
                Part(side+"Sneaker",PrimitiveType.Sphere,t,new Vector3(s*.25f,.18f,-.1f),new Vector3(.40f,.25f,.59f),shoe);
                Part(side+"ShoeStripe",PrimitiveType.Sphere,t,new Vector3(s*.25f,.24f,-.21f),new Vector3(.31f,.065f,.31f),cream);
                Part(side+"Trouser",PrimitiveType.Capsule,t,new Vector3(s*.23f,.60f,0),new Vector3(.34f,.39f,.35f),pants);
                var sleeve=Part("Hoodie"+side+"Sleeve",PrimitiveType.Capsule,t,new Vector3(s*.57f,1.24f,0),new Vector3(.34f,.33f,.36f),hoodie);
                sleeve.transform.localRotation=Quaternion.Euler(0,0,s*20);
                Part(side+"Hand",PrimitiveType.Sphere,t,new Vector3(s*.70f,.91f,-.035f),new Vector3(.25f,.28f,.25f),skin);
                Part(side+"Ear",PrimitiveType.Sphere,t,new Vector3(s*.45f,2.06f,0),new Vector3(.20f,.29f,.19f),skin);
            }
            // Тело толстовки, низ, капюшон и карман используют один материал одежды.
            Part("HoodieBody",PrimitiveType.Sphere,t,new Vector3(0,1.30f,0),new Vector3(.92f,1.02f,.67f),hoodie);
            Part("HoodieHem",PrimitiveType.Sphere,t,new Vector3(0,.91f,-.015f),new Vector3(.77f,.18f,.64f),hoodie);
            Part("HoodieHood",PrimitiveType.Sphere,t,new Vector3(0,1.73f,.12f),new Vector3(.76f,.45f,.69f),hoodie);
            Part("HoodiePocket",PrimitiveType.Sphere,t,new Vector3(0,1.13f,-.31f),new Vector3(.51f,.22f,.08f),hoodie);
            for(var s=-1;s<=1;s+=2)
                Part("Drawstring"+s,PrimitiveType.Capsule,t,new Vector3(s*.11f,1.51f,-.3f),new Vector3(.032f,.14f,.032f),cream);
            // Голова, волосы и небольшие детали лица делают героя узнаваемым.
            Part("Neck",PrimitiveType.Capsule,t,new Vector3(0,1.72f,0),new Vector3(.25f,.17f,.25f),skin);
            Part("Head",PrimitiveType.Sphere,t,new Vector3(0,2.10f,0),new Vector3(.90f,1.00f,.84f),skin);
            Part("HairCap",PrimitiveType.Sphere,t,new Vector3(0,2.45f,.065f),new Vector3(.91f,.46f,.82f),hair);
            for(int i=0;i<4;i++)
            {
                var tuft=Part("HairTuft"+i,PrimitiveType.Sphere,t,new Vector3(-.29f+i*.17f,2.46f-i*.028f,-.26f),new Vector3(.30f,.27f,.30f),hair);
                tuft.transform.localRotation=Quaternion.Euler(0,0,-25);
            }
            for(var s=-1;s<=1;s+=2)
            {
                Part("EyeWhite"+s,PrimitiveType.Sphere,t,new Vector3(s*.17f,2.16f,-.38f),new Vector3(.22f,.25f,.10f),cream);
                Part("Pupil"+s,PrimitiveType.Sphere,t,new Vector3(s*.16f,2.15f,-.436f),new Vector3(.10f,.14f,.040f),iris);
                Part("EyeShine"+s,PrimitiveType.Sphere,t,new Vector3(s*.16f-.02f,2.19f,-.46f),new Vector3(.029f,.04f,.015f),cream);
                var brow=Part("Brow"+s,PrimitiveType.Sphere,t,new Vector3(s*.18f,2.34f,-.35f),new Vector3(.23f,.059f,.05f),hair);
                brow.transform.localRotation=Quaternion.Euler(0,0,s*7);
                Part("Cheek"+s,PrimitiveType.Sphere,t,new Vector3(s*.26f,1.98f,-.335f),new Vector3(.12f,.074f,.032f),rosy);
            }
            Part("Nose",PrimitiveType.Sphere,t,new Vector3(0,2.04f,-.42f),new Vector3(.14f,.15f,.13f),skin);
            Part("Smile",PrimitiveType.Sphere,t,new Vector3(0,1.91f,-.36f),new Vector3(.23f,.095f,.047f),hair);
            Part("SmileTeeth",PrimitiveType.Sphere,t,new Vector3(0,1.932f,-.388f),new Vector3(.16f,.034f,.018f),cream);
            // Область нажатия охватывает героя; камера направляет события мыши компоненту вращения.
            var hitArea = root.AddComponent<BoxCollider>();
            hitArea.center = new Vector3(0, 1.3f, 0);
            hitArea.size = new Vector3(1.65f, 2.65f, .95f);
            root.AddComponent<CharacterIdle>();
            root.AddComponent<CharacterDrag>();
            CharacterRigBuilder.Configure(root);
            // Сохраняем шаблон с покачиванием и вращением. Временную модель затем удаляем.
            var asset=PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/LobbyCharacter.prefab"); UnityEngine.Object.DestroyImmediate(root); return asset;
        }
        /// <summary>
        /// Создаёт прямоугольную область интерфейса.
        /// </summary>
        /// <param name="name">Имя элемента.</param>
        /// <param name="parent">Элемент, внутри которого размещается область.</param>
        /// <param name="anchor">Якорь — точка привязки к родителю от 0 до 1.</param>
        /// <param name="position">Отступ от якоря.</param>
        /// <param name="size">Ширина и высота.</param>
        /// <param name="pivot">Опорная точка самой области; по умолчанию центр.</param>
        /// <returns>RectTransform — компонент положения и размера области.</returns>
        static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Vector2? pivot=null)
        {
            var go=new GameObject(name,typeof(RectTransform)); var rt=go.GetComponent<RectTransform>(); rt.SetParent(parent,false);
            rt.anchorMin=rt.anchorMax=anchor; rt.pivot=pivot ?? new Vector2(.5f,.5f); rt.anchoredPosition=position; rt.sizeDelta=size; return rt;
        }
        /// <summary>
        /// Добавляет цветной прямоугольник, который по умолчанию не перехватывает мышь.
        /// </summary>
        /// <param name="rt">Область для изображения.</param>
        /// <param name="color">Цвет прямоугольника.</param>
        /// <returns>Компонент изображения.</returns>
        static Image Image(RectTransform rt, Color color)
        { var image=rt.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;return image; }
        /// <summary>
        /// Создаёт надпись со шрифтом, содержащим русские буквы.
        /// </summary>
        /// <param name="name">Имя элемента.</param>
        /// <param name="parent">Родительский элемент.</param>
        /// <param name="copy">Текст надписи.</param>
        /// <param name="anchor">Точка привязки к родителю.</param>
        /// <param name="pos">Отступ от точки привязки.</param>
        /// <param name="size">Размер области текста.</param>
        /// <param name="fontSize">Размер букв.</param>
        /// <param name="color">Цвет текста.</param>
        /// <param name="heavy">Использовать ли жирный шрифт.</param>
        /// <param name="align">Выравнивание текста внутри области.</param>
        /// <returns>Компонент надписи.</returns>
        static Text Text(string name, Transform parent, string copy, Vector2 anchor, Vector2 pos, Vector2 size, int fontSize, Color color, bool heavy=false, TextAnchor align=TextAnchor.MiddleLeft)
        {
            var rt=Rect(name,parent,anchor,pos,size);var text=rt.gameObject.AddComponent<Text>();text.text=copy;text.font=heavy?bold:regular;text.fontSize=fontSize;text.color=color;text.alignment=align;
            text.raycastTarget=false;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;return text;
        }
        /// <summary>
        /// Создаёт кнопку с надписью и видимыми состояниями нажатия и выделения.
        /// </summary>
        /// <param name="name">Имя кнопки.</param>
        /// <param name="parent">Родительский элемент.</param>
        /// <param name="copy">Текст на кнопке.</param>
        /// <param name="anchor">Точка привязки к родителю.</param>
        /// <param name="pos">Отступ от точки привязки.</param>
        /// <param name="size">Размер кнопки.</param>
        /// <param name="color">Цвет фона.</param>
        /// <param name="fontSize">Размер букв.</param>
        /// <param name="labelColor">Цвет надписи; по умолчанию белый.</param>
        /// <returns>Компонент кнопки.</returns>
        static Button Button(string name,Transform parent,string copy,Vector2 anchor,Vector2 pos,Vector2 size,Color color,int fontSize=24,Color? labelColor=null)
        {
            var rt=Rect(name,parent,anchor,pos,size); var image=Image(rt,color); image.raycastTarget=true;
            var b=rt.gameObject.AddComponent<Button>();b.targetGraphic=image;
            // Меняем оттенок при наведении мыши, выборе клавиатурой и нажатии.
            var colors=b.colors; colors.normalColor=Color.white;colors.highlightedColor=new Color(1.15f,1.15f,1.15f); colors.selectedColor=new Color(1.12f,1.12f,1.12f);colors.pressedColor=new Color(.72f,.82f,.94f);colors.disabledColor=new Color(.4f,.4f,.4f);colors.fadeDuration=.12f;b.colors=colors;
            b.navigation=new Navigation{mode=Navigation.Mode.Explicit};
            Text(name+"Label",rt,copy,new Vector2(.5f,.5f),Vector2.zero,size-new Vector2(16,4),fontSize,labelColor??Color.white,true,TextAnchor.MiddleCenter);
            return b;
        }
        /// <summary>
        /// Сохраняет ссылку на один объект в поле контроллера.
        /// </summary>
        /// <param name="so">SerializedObject — доступ к полям компонента в редакторе Unity.</param>
        /// <param name="name">Имя поля.</param>
        /// <param name="value">Объект, который нужно связать с полем.</param>
        static void Assign(SerializedObject so,string name,UnityEngine.Object value) { so.FindProperty(name).objectReferenceValue=value; }
        /// <summary>
        /// Сохраняет несколько ссылок в массиве — списке объектов контроллера.
        /// </summary>
        /// <param name="so">Доступ к полям компонента в редакторе Unity.</param>
        /// <param name="name">Имя поля-массива.</param>
        /// <param name="values">Объекты, которые нужно сохранить.</param>
        static void AssignArray(SerializedObject so,string name,IEnumerable<UnityEngine.Object> values)
        {
            var array=values.ToArray();var p=so.FindProperty(name);p.arraySize=array.Length;
            for(int i=0;i<array.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=array[i];
        }
        /// <summary>
        /// Собирает кнопки и панели поверх 3D-сцены и связывает их с контроллером.
        /// </summary>
        /// <param name="hero">Герой, у которого находим детали толстовки для смены цвета.</param>
        /// <returns>Корневой объект интерфейса.</returns>
        static GameObject MakeUI(GameObject hero)
        {
            // Canvas — поверхность для интерфейса. Overlay рисует её поверх персонажа.
            var canvasGo=new GameObject("LobbyUI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            var canvas=canvasGo.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            // Размеры рассчитаны на 1920×1080; CanvasScaler подстраивает их под окно.
            var scaler=canvasGo.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            var t=canvasGo.transform; var controller=canvasGo.AddComponent<LobbyController>();
            // Верхняя полоса содержит название игры и три вкладки.
            var header=Rect("Header",t,new Vector2(.5f,1),new Vector2(0,-61),new Vector2(1920,122)); Image(header,new Color(.025f,.048f,.10f,.92f)); header.anchorMin=new Vector2(0,1); header.anchorMax=Vector2.one; header.sizeDelta=new Vector2(0,122);
            Text("Brand",header,"TINY TROUBLE",new Vector2(0,.5f),new Vector2(228,8),new Vector2(350,44),31,Color.white,true);
            Text("BrandCaption",header,"МАЛЕНЬКИЕ НЕПРИЯТНОСТИ",new Vector2(0,.5f),new Vector2(228,-25),new Vector2(350,22),12,Cyan,true);
            var tabs=new Button[3];string[] ids={"TabPlay","TabCharacter","TabSettings"};string[] labels={"ИГРАТЬ","ПЕРСОНАЖ","НАСТРОЙКИ"};
            for(int i=0;i<3;i++)
            {
                tabs[i]=Button(ids[i],header,labels[i],new Vector2(.5f,.5f),new Vector2(-220+i*220,0),new Vector2(204,54),Ink,20);
                // Сохраняем связь кнопки с методом, чтобы она работала после открытия сцены заново.
                UnityEventTools.AddIntPersistentListener(tabs[i].onClick,controller.SelectTab,i);
                tabs[i].navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnLeft=tabs[Mathf.Max(0,i-1)]};
            }
            for(int i=0;i<3;i++) { var n=tabs[i].navigation;n.selectOnLeft=tabs[(i+2)%3];n.selectOnRight=tabs[(i+1)%3];tabs[i].navigation=n; }
            Text("LobbyTag",header,"ЛОББИ",new Vector2(1,.5f),new Vector2(-145,0),new Vector2(150,38),16,Cyan,true,TextAnchor.MiddleRight);
            // Слева — описание, над головой — имя героя, снизу — подсказка клавиш.
            var info=Rect("Intro",t,new Vector2(0,.5f),new Vector2(68,80),new Vector2(420,320),new Vector2(0,.5f));
            Text("Eyebrow",info,"ТВОЯ ИСТОРИЯ НАЧИНАЕТСЯ ЗДЕСЬ",new Vector2(0,1),new Vector2(194,-20),new Vector2(388,24),14,Cyan,true);
            Text("IntroTitle",info,"МАЛЕНЬКИЙ\nГЕРОЙ.\nБОЛЬШИЕ\nПРИКЛЮЧЕНИЯ.",new Vector2(0,1),new Vector2(196,-134),new Vector2(392,212),37,Color.white,true);
            Text("IntroCopy",info,"Выбери свой цвет.\nИ отправляйся навстречу неприятностям.",new Vector2(0,0),new Vector2(192,-27),new Vector2(384,70),18,Muted);
            var playerTag=Rect("PlayerTag",t,new Vector2(.5f,.5f),new Vector2(0,360),new Vector2(246,62)); Image(playerTag,new Color(.025f,.05f,.12f,.72f));
            Text("HeroName",playerTag,"НОВЫЙ ГЕРОЙ",new Vector2(.5f,.5f),new Vector2(0,8),new Vector2(230,27),19,Color.white,true,TextAnchor.MiddleCenter);
            Text("HeroState",playerTag,"ГОТОВ К ПРИКЛЮЧЕНИЯМ",new Vector2(.5f,.5f),new Vector2(0,-16),new Vector2(230,20),10,Cyan,true,TextAnchor.MiddleCenter);
            Text("Footer",t,"TINY TROUBLE  /  ПЕРВОЕ ПРИКЛЮЧЕНИЕ",new Vector2(0,0),new Vector2(326,35),new Vector2(520,28),13,Muted);
            Text("KeyboardHint",t,"TAB — ВЫБОР    ENTER — ПОДТВЕРДИТЬ",new Vector2(1,0),new Vector2(-329,35),new Vector2(530,28),12,Muted,false,TextAnchor.MiddleRight);
            Text("RotationHint",t,"Зажми и потяни, чтобы повернуть",new Vector2(.5f,0),new Vector2(0,35),new Vector2(650,28),14,Cyan,false,TextAnchor.MiddleCenter);
            // Три панели находятся справа в одной позиции. Контроллер показывает только нужную.
            var panels=new GameObject[3];
            for(int i=0;i<3;i++)
            {
                string[] names={"PlayPanel","CharacterPanel","SettingsPanel"};
                var p=Rect(names[i],t,new Vector2(1,0),new Vector2(-68,102),new Vector2(425,350),new Vector2(1,0));
                Image(p,new Color(.027f,.063f,.14f,.94f)); panels[i]=p.gameObject;
                var accent=Rect("Accent",p,new Vector2(.5f,1),new Vector2(0,-2),new Vector2(425,4));Image(accent,Cyan);
            }
            // Панель «Играть»: большая кнопка и сообщение о состоянии загрузки.
            var play=panels[0].transform;
            Text("PlayEyebrow",play,"ТВОЁ СЛЕДУЮЩЕЕ ПРИКЛЮЧЕНИЕ",new Vector2(.5f,1),new Vector2(0,-40),new Vector2(369,28),13,Cyan,true);
            Text("PlayTitle",play,"ГОТОВ К СТАРТУ?",new Vector2(.5f,1),new Vector2(0,-94),new Vector2(369,58),32,Color.white,true);
            var status=Text("PlayStatus",play,"Новый мир ждёт тебя",new Vector2(.5f,1),new Vector2(0,-145),new Vector2(369,45),17,Muted);
            var playButton=Button("PlayButton",play,"ИГРАТЬ  →",new Vector2(.5f,0),new Vector2(0,82),new Vector2(369,94),Yellow,36,Ink);
            UnityEventTools.AddPersistentListener(playButton.onClick,controller.Play);
            Text("PlayHint",play,"НАЧНИ СВОЁ ПРИКЛЮЧЕНИЕ",new Vector2(.5f,0),new Vector2(0,21),new Vector2(369,22),11,Muted,true,TextAnchor.MiddleCenter);
            // Панель «Персонаж»: четыре цвета и белая отметка выбранного варианта.
            var character=panels[1].transform;
            Text("CharacterTitle",character,"ТВОЙ СТИЛЬ",new Vector2(.5f,1),new Vector2(0,-61),new Vector2(369,54),32,Color.white,true);
            Text("CharacterCopy",character,"Выбери цвет толстовки",new Vector2(.5f,1),new Vector2(0,-116),new Vector2(369,38),18,Muted);
            string[] colors={"#FFD84A","#46C8F5","#FF646B","#AA79FF"};var colorButtons=new Button[4];
            for(int i=0;i<4;i++)
            {
                colorButtons[i]=Button("Color"+i,character,"",new Vector2(.5f,.5f),new Vector2(-132+i*88,-24),new Vector2(70,74),Hex(colors[i]));
                var frame=Rect("SelectionFrame",colorButtons[i].transform,new Vector2(.5f,.5f),new Vector2(0,-48),new Vector2(70,5));Image(frame,Color.white);frame.gameObject.SetActive(i==0);
                UnityEventTools.AddIntPersistentListener(colorButtons[i].onClick,controller.SetHoodieColor,i);
            }
            for(int i=0;i<4;i++)colorButtons[i].navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnLeft=colorButtons[(i+3)%4],selectOnRight=colorButtons[(i+1)%4]};
            Text("StyleHint",character,"Твой выбор сохраняется автоматически",new Vector2(.5f,0),new Vector2(0,47),new Vector2(369,52),14,Muted,false,TextAnchor.MiddleCenter);
            // Панель «Настройки»: процент и ползунок громкости от 0 до 1.
            var settings=panels[2].transform;
            Text("SettingsTitle",settings,"НАСТРОЙКИ",new Vector2(.5f,1),new Vector2(0,-61),new Vector2(369,54),32,Color.white,true);
            Text("VolumeLabel",settings,"ОБЩАЯ ГРОМКОСТЬ",new Vector2(.5f,1),new Vector2(-38,-128),new Vector2(293,35),15,Muted,true);
            var volumeText=Text("VolumePercent",settings,"100%",new Vector2(1,1),new Vector2(-70,-128),new Vector2(85,35),20,Color.white,true,TextAnchor.MiddleRight);
            var sliderRt=Rect("VolumeSlider",settings,new Vector2(.5f,.5f),new Vector2(0,-35),new Vector2(355,50));
            // Прозрачная область принимает нажатия по всей шкале, а не только по бегунку.
            Image(sliderRt,new Color(0,0,0,0)).raycastTarget=true;
            var track=Rect("Track",sliderRt,new Vector2(.5f,.5f),Vector2.zero,new Vector2(355,10));Image(track,Hex("#2C4466"));
            var fillArea=Rect("FillArea",sliderRt,new Vector2(.5f,.5f),Vector2.zero,new Vector2(355,10));
            var fillRect=Rect("Fill",fillArea,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero);fillRect.anchorMin=Vector2.zero;fillRect.anchorMax=Vector2.one;Image(fillRect,Cyan);
            var handleArea=Rect("HandleArea",sliderRt,new Vector2(.5f,.5f),Vector2.zero,new Vector2(335,50));
            var handle=Rect("Handle",handleArea,new Vector2(.5f,.5f),Vector2.zero,new Vector2(18,32));var handleImage=Image(handle,Color.white);handleImage.raycastTarget=true;
            var slider=sliderRt.gameObject.AddComponent<Slider>();slider.fillRect=fillRect;slider.handleRect=handle;slider.targetGraphic=handleImage;slider.minValue=0;slider.maxValue=1;slider.value=1;
            slider.navigation=new Navigation{mode=Navigation.Mode.Explicit};UnityEventTools.AddPersistentListener(slider.onValueChanged,controller.SetVolume);
            Text("VolumeHint",settings,"Меньше шума. Больше приключений.\nНастройки сохраняются автоматически.",new Vector2(.5f,0),new Vector2(0,51),new Vector2(369,62),14,Muted,false,TextAnchor.MiddleCenter);
            // EventSystem передаёт нажатия интерфейсу. Ниже сохраняем ссылки для контроллера.
            new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
            var so=new SerializedObject(controller);
            AssignArray(so,"panels",panels);AssignArray(so,"tabs",tabs);Assign(so,"playButton",playButton);Assign(so,"statusText",status);
            Assign(so,"volumeSlider",slider);Assign(so,"volumeText",volumeText);AssignArray(so,"colorButtons",colorButtons);
            // Выбираем только детали с именем Hoodie, чтобы не менять цвет кожи и штанов.
            AssignArray(so,"hoodieRenderers",hero.GetComponentsInChildren<Renderer>().Where(r=>r.name.StartsWith("Hoodie")));
            Assign(so,"characterLife",hero.GetComponent<CharacterLife>());
            so.ApplyModifiedPropertiesWithoutUndo();
            // В сохранённой сцене сразу оставляем открытой только вкладку «Играть».
            for(int i=0;i<3;i++)panels[i].SetActive(i==0);
            // Отдельный компонент переставляет меню и камеру при изменении размера окна.
            canvasGo.AddComponent<LobbyLayout>().Configure(Camera.main, hero.transform);
            return canvasGo;
        }
    }
}
