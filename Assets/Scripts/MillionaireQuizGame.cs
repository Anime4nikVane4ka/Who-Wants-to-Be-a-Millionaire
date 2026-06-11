using System.Collections;
using System.Globalization;
using extOSC;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.Video;

public sealed class MillionaireQuizGame : MonoBehaviour
{
    private const int QuestionsCount = 8;
    private const int AnswersCount = 4;
    private const bool StartOnFinalScreenForTesting = false;
    private const string DebugStartQuestion = "8";
    private const bool DebugForceSuperGameAccess = true;
    private const float QuestionBackgroundCogRotationSpeed = -4f;
    private const string DefaultOscHost = "127.0.0.1";
    private const int DefaultOscPort = 9000;
    private const string OscHostPrefsKey = "MillionaireOscHost";
    private const string OscPortPrefsKey = "MillionaireOscPort";
    private const string OscGameStartAddress = "/game/GAME_START";
    private const string OscAnswerCorrectAddress = "/game/ANSWER_CORRECT";
    private const string OscAnswerWrongAddress = "/game/ANSWER_WRONG";
    private const string OscGameEndAddress = "/game/GAME_END";

    private static readonly Color BackgroundColor = new Color32(0, 91, 168, 255);
    private static readonly Color PanelColor = new Color32(254, 254, 254, 255);
    private static readonly Color ButtonColor = new Color32(254, 254, 254, 255);
    private static readonly Color ButtonHoverColor = new Color32(254, 254, 254, 255);
    private static readonly Color SelectedAnswerBlinkColor = new Color32(92, 183, 255, 255);
    private static readonly Color AccentColor = new Color32(254, 254, 254, 255);
    private static readonly Color TextColor = new Color32(254, 254, 254, 255);
    private static readonly Color FrameTextColor = new Color32(0, 91, 168, 255);
    private static readonly Color MutedTextColor = new Color32(254, 254, 254, 255);
    private static readonly Color QrLightColor = new Color32(255, 255, 255, 255);
    private static readonly Color QrDarkColor = new Color32(0, 0, 0, 255);

    private readonly QuestionData[] questions = new QuestionData[QuestionsCount];
    private QuestionData superQuestion;
    private Button[] answerButtons;
    private Text[] answerLabels;
    private RectTransform questionPanel;
    private RectTransform questionBackgroundCog;
    private RectTransform correctFeedbackShimmer;
    private Text questionLabel;
    private Text progressLabel;
    private Text feedbackTitleLabel;
    private CanvasGroup feedbackPanelCanvasGroup;
    private Text finalScoreLabel;
    private Text finalRewardLabel;
    private GameObject startScreen;
    private GameObject questionScreen;
    private GameObject superGameIntroScreen;
    private GameObject feedbackScreen;
    private GameObject finalScreen;
    private GameObject homeConfirmationOverlay;
    private Image superLogoImage;
    private Image superGameIntroLogoImage;
    private RawImage superGameConfettiImage;
    private RawImage feedbackConfettiImage;
    private Button startButton;
    private GameObject oscSettingsPanel;
    private InputField oscHostInput;
    private InputField oscPortInput;
    private CanvasGroup questionScreenCanvasGroup;
    private CanvasGroup finalScreenCanvasGroup;

    private Font regularFont;
    private Font boldFont;
    private Sprite logoSprite;
    private Sprite pmufLogoSprite;
    private Sprite minuLogoSprite;
    private Sprite superLogoSprite;
    private Sprite superGameLogoSprite;
    private Sprite[] countdownLogoSprites;
    private Sprite roundedRectSprite;
    private Sprite goldShimmerSprite;
    private Sprite questionScreenFieldSprite;
    private Sprite millionaireQuestionSprite;
    private Sprite millionaireAnswerSprite;
    private Sprite feedbackShimmerMaskSprite;
    private Sprite questionBackgroundCogSprite;
    private Texture2D[] superGameConfettiFrames;
    private float[] superGameConfettiFrameDurations;
    private Material feedbackShimmerMaskMaterial;
    private AudioSource audioSource;
    private AudioSource correctAudioSource;
    private AudioSource incorrectAudioSource;
    private AudioSource finalRoundAudioSource;
    private AudioClip countdownSoundClip;
    private AudioClip clickSoundClip;
    private AudioClip correctSoundClip;
    private AudioClip incorrectSoundClip;
    private AudioClip finalRoundSoundClip;
    private AudioClip finalScreenSoundClip;
    private OSCTransmitter oscTransmitter;
    private VideoPlayer backgroundVideoPlayer;
    private RenderTexture backgroundVideoTexture;
    private int currentQuestionIndex;
    private int correctAnswers;
    private int selectedAnswerIndex;
    private int superGameConfettiFrameIndex;
    private int feedbackConfettiFrameIndex;
    private float superGameConfettiFrameTimer;
    private float feedbackConfettiFrameTimer;
    private bool answerLocked;
    private bool superGameActive;
    private bool superGamePlayed;
    private bool selectedAnswerWasCorrect;
    private bool feedbackWasInterruptedByHomeConfirmation;
    private bool gameSessionActive;
    private string oscRemoteHost;
    private int oscRemotePort;
    private Coroutine feedbackRoutine;
    private Coroutine countdownRoutine;
    private Coroutine questionScreenFadeRoutine;
    private Coroutine finalScreenFadeRoutine;
    private Coroutine superGameIntroRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<MillionaireQuizGame>() != null)
        {
            return;
        }

        GameObject quizGame = new GameObject("Millionaire Quiz Game");
        quizGame.AddComponent<MillionaireQuizGame>();
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        LoadOscSettings();
        LoadBrandAssets();
        CreateAudioSource();
        CreateOscTransmitter();
        CreateQuestions();
        CreateEventSystem();
        CreateInterface();

        if (StartOnFinalScreenForTesting)
        {
            ShowFinalScreen();
        }
        else
        {
            ShowStartScreen();
        }
    }

    private void OnDestroy()
    {
        if (backgroundVideoPlayer != null)
        {
            backgroundVideoPlayer.loopPointReached -= RestartBackgroundVideo;
        }

        if (backgroundVideoTexture != null)
        {
            backgroundVideoTexture.Release();
            Destroy(backgroundVideoTexture);
        }

        if (feedbackShimmerMaskMaterial != null)
        {
            Destroy(feedbackShimmerMaskMaterial);
        }
    }

    private void Update()
    {
        if (questionBackgroundCog == null || questionScreen == null || !questionScreen.activeSelf)
        {
            return;
        }

        questionBackgroundCog.Rotate(0f, 0f, QuestionBackgroundCogRotationSpeed * Time.deltaTime);
    }

    private void CreateQuestions()
    {
        questions[0] = new QuestionData
        {
            Text = "Соглашение сторон о порядке разрешения возможных споров это:",
            CorrectAnswerIndex = 3,
            Answers = new[] { "(А) Народная поговорка", "(Б) Юридическая скороговорка", "(В) Правовая отговорка", "(Г) Арбитражная оговорка" }
        };

        questions[1] = new QuestionData
        {
            Text = "За нарушение обязательства может наступить:",
            CorrectAnswerIndex = 0,
            Answers = new[] { "(А) Ответственность", "(Б) Безответственность", "(В) Посредственность", "(Г) Собственность" }
        };

        questions[2] = new QuestionData
        {
            Text = "Что у юристов означает выражение:\n«Применить триста тридцать третью»?",
            CorrectAnswerIndex = 1,
            Answers = new[] { "(А) Снижение арбитражного сбора", "(Б) Снижение размера неустойки", "(В) Снижение цены иска", "(Г) Снижение наказания ниже низшего предела" }
        };

        questions[3] = new QuestionData
        {
            Text = "Что такое третейский суд?",
            CorrectAnswerIndex = 2,
            Answers = new[] { "(А) Постоянно действующее\nарбитражное учреждение", "(Б) Рекомендованный список арбитров", "(В) Единоличный арбитр или коллегия арбитров", "(Г) Арбитражный суд" }
        };

        questions[4] = new QuestionData
        {
            Text = "Принцип беспристрастности относится к:",
            CorrectAnswerIndex = 3,
            Answers = new[] { "(А) Сторонам спора", "(Б) Арбитражной оговорке", "(В) Арбитражному учреждению", "(Г) Арбитру" }
        };

        questions[5] = new QuestionData
        {
            Text = "Какова правовая природа арбитража?",
            CorrectAnswerIndex = 3,
            Answers = new[] { "(А) Договорная", "(Б) Процессуальная", "(В) Смешанная", "(Г) Чтобы разобраться нужно оформить подписку на журнал «Третейский суд»" }
        };

        questions[6] = new QuestionData
        {
            Text = "В договоре поставки отсутствует соглашение сторон о неустойке, но истец требует ее взыскать.\nВ этом случае третейский суд должен:",
            CorrectAnswerIndex = 1,
            Answers = new[] { "(А) Отказать во взыскании", "(Б) Взыскать неустойку в размере\nключевой ставки ЦБ РФ", "(В) Взыскать неустойку в двойном\nразмере ключевой ставки ЦБ РФ", "(Г) Третейский суд никому ничего не\nдолжен. Он ведь третейский суд" }
        };

        questions[7] = new QuestionData
        {
            Text = "Право стороны на односторонний отказ от исполнения обязательства может быть реализовано:",
            CorrectAnswerIndex = 2,
            Answers = new[] { "(А) Если это право предусмотрено договором", "(Б) Если это право предусмотрено\nзаконом или договором", "(В) Разумно и добросовестно,\nесли это право предусмотрено\nзаконом или договором", "(Г) Обязательство должно\nисполняться в любом случае" }
        };

        superQuestion = new QuestionData
        {
            Text = "Этот ученый правовед в 1981 году защитил кандидатскую диссертацию\nна тему: «Динамика обязательственного правоотношения и\nгражданско-правовая ответственность», а в 1994 году защитил\nдокторскую диссертацию на тему «Проблемы правового\nрежима предпринимательства». Назовите этого ученого:",
            CorrectAnswerIndex = 1,
            Answers = new[] { "(А) Евгений Борисович Хохлов", "(Б) Владимир Фёдорович Попондопуло", "(Г) Константин Константинович Лебедев", "(Д) Валерий Абрамович Мусин" }
        };
    }

    private void CreateEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        DontDestroyOnLoad(eventSystem);
        eventSystem.AddComponent<EventSystem>();
        InputSystemUIInputModule inputModule = eventSystem.AddComponent<InputSystemUIInputModule>();
        inputModule.AssignDefaultActions();
    }

    private void CreateInterface()
    {
        GameObject canvasObject = new GameObject("Millionaire Canvas");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        Image background = canvasObject.AddComponent<Image>();
        background.type = Image.Type.Simple;
        background.preserveAspect = false;
        background.color = BackgroundColor;
        background.raycastTarget = false;

        CreateBackgroundVideo(canvasObject.transform);

        startScreen = CreateScreen(canvasObject.transform, "Start Screen");
        CreateStartScreen(startScreen.transform);

        questionScreen = CreateScreen(canvasObject.transform, "Question Screen");
        questionScreenCanvasGroup = questionScreen.AddComponent<CanvasGroup>();
        CreateQuestionScreen(questionScreen.transform);

        superGameIntroScreen = CreateScreen(canvasObject.transform, "Super Game Intro Screen");
        CreateSuperGameIntroScreen(superGameIntroScreen.transform);

        feedbackScreen = CreateScreen(canvasObject.transform, "Feedback Screen");
        CreateFeedbackScreen(feedbackScreen.transform);

        finalScreen = CreateScreen(canvasObject.transform, "Final Screen");
        finalScreenCanvasGroup = finalScreen.AddComponent<CanvasGroup>();
        CreateFinalScreen(finalScreen.transform);

        homeConfirmationOverlay = CreateScreen(canvasObject.transform, "Home Confirmation Overlay");
        CreateHomeConfirmationOverlay(homeConfirmationOverlay.transform);
        homeConfirmationOverlay.SetActive(false);
    }

    private void CreateStartScreen(Transform parent)
    {
        CreateLogo(parent);
        CreateOscSettingsControls(parent);

        superLogoImage = CreateLogoImage(parent, superLogoSprite, "Super Logo", new Vector2(0.5f, 0.47f), new Vector2(1180f * 2f, 270f * 2f));

        startButton = CreateMillionaireAnswerButton(parent, "Start Button", new Vector2(0.5f, 0.12f), new Vector2(620f, 153f), StartCountdown);
        Text startButtonLabel = startButton.GetComponentInChildren<Text>();
        startButtonLabel.text = "Начать";
        startButtonLabel.fontSize = 70;
    }

    private void CreateOscSettingsControls(Transform parent)
    {
        Button settingsButton = CreateButton(parent, "OSC Settings Button", "OSC", new Vector2(0.95f, 0.94f), new Vector2(96f, 52f), () =>
        {
            PlaySound(clickSoundClip);
            ToggleOscSettingsPanel();
        });
        settingsButton.GetComponentInChildren<Text>().fontSize = 22;

        RectTransform panel = CreatePanel(parent, "OSC Settings Panel", new Vector2(0.82f, 0.73f), new Vector2(620f, 310f), PanelColor);
        oscSettingsPanel = panel.gameObject;

        CreateText(panel, "OSC Settings Title", "OSC настройки", 34, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.82f), new Vector2(540f, 48f), FrameTextColor);

        CreateText(panel, "OSC Host Label", "Адрес", 26, FontStyle.Bold, TextAnchor.MiddleLeft,
            new Vector2(0.19f, 0.61f), new Vector2(160f, 44f), FrameTextColor);
        oscHostInput = CreateInputField(panel, "OSC Host Input", oscRemoteHost, new Vector2(0.62f, 0.61f), new Vector2(330f, 52f), InputField.ContentType.Standard);

        CreateText(panel, "OSC Port Label", "Порт", 26, FontStyle.Bold, TextAnchor.MiddleLeft,
            new Vector2(0.19f, 0.39f), new Vector2(160f, 44f), FrameTextColor);
        oscPortInput = CreateInputField(panel, "OSC Port Input", oscRemotePort.ToString(), new Vector2(0.62f, 0.39f), new Vector2(330f, 52f), InputField.ContentType.IntegerNumber);

        Button saveButton = CreateButton(panel, "OSC Save Button", "Сохранить", new Vector2(0.5f, 0.16f), new Vector2(270f, 62f), () =>
        {
            PlaySound(clickSoundClip);
            SaveOscSettingsFromInputs();
        });
        saveButton.GetComponentInChildren<Text>().fontSize = 28;

        oscSettingsPanel.SetActive(false);
    }

    private void CreateSuperGameIntroScreen(Transform parent)
    {
        CreateSuperGameConfetti(parent);
        superGameIntroLogoImage = CreateLogoImage(parent, superGameLogoSprite, "Super Game Logo", new Vector2(0.5f, 0.47f), new Vector2(1180f * 4f, 270f * 4f));
    }

    private void CreateSuperGameConfetti(Transform parent)
    {
        if (superGameConfettiFrames == null || superGameConfettiFrames.Length == 0)
        {
            return;
        }

        GameObject confettiObject = new GameObject("Super Game Confetti");
        confettiObject.transform.SetParent(parent, false);

        RectTransform rectTransform = confettiObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        superGameConfettiImage = confettiObject.AddComponent<RawImage>();
        superGameConfettiImage.texture = superGameConfettiFrames[0];
        superGameConfettiImage.color = Color.white;
        superGameConfettiImage.raycastTarget = false;
        confettiObject.SetActive(false);
    }

    private void CreateBackgroundVideo(Transform parent)
    {
        VideoClip backgroundVideoClip = Resources.Load<VideoClip>("Video/flag_blue_loop_seamless_forward_50fps");

        if (backgroundVideoClip == null)
        {
            Debug.LogWarning("Background video clip was not found in Resources/Video.");
            return;
        }

        GameObject videoObject = new GameObject("Background Video");
        videoObject.transform.SetParent(parent, false);

        RectTransform rectTransform = videoObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        backgroundVideoTexture = new RenderTexture(1920, 1080, 0);
        backgroundVideoTexture.name = "Background Video Render Texture";
        backgroundVideoTexture.Create();

        RawImage image = videoObject.AddComponent<RawImage>();
        image.texture = backgroundVideoTexture;
        image.raycastTarget = false;

        backgroundVideoPlayer = videoObject.AddComponent<VideoPlayer>();
        backgroundVideoPlayer.playOnAwake = false;
        backgroundVideoPlayer.isLooping = true;
        backgroundVideoPlayer.renderMode = VideoRenderMode.RenderTexture;
        backgroundVideoPlayer.targetTexture = backgroundVideoTexture;
        backgroundVideoPlayer.aspectRatio = VideoAspectRatio.FitOutside;
        backgroundVideoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        backgroundVideoPlayer.playbackSpeed = 1f;
        backgroundVideoPlayer.skipOnDrop = true;
        backgroundVideoPlayer.clip = backgroundVideoClip;
        backgroundVideoPlayer.loopPointReached += RestartBackgroundVideo;
        backgroundVideoPlayer.Play();
    }

    private void RestartBackgroundVideo(VideoPlayer player)
    {
        player.frame = 0;
        player.Play();
    }

    private void CreateLogo(Transform parent)
    {
        CreateLogoRow(parent, 0.85f, 1.5f);
    }

    private void CreateQuestionHeaderLogos(Transform parent)
    {
        CreateLogoRow(parent, 0.89f, 1.3f);
    }

    private void CreateLogoRow(Transform parent, float anchorY, float logoMultiplier)
    {
        CreateLogoImage(parent, pmufLogoSprite, "PMUF Logo", new Vector2(0.21f, anchorY), new Vector2(360f * logoMultiplier, 150f * logoMultiplier));
        CreateLogoImage(parent, minuLogoSprite, "Brand Logo", new Vector2(0.52f, anchorY), new Vector2(360f * logoMultiplier, 150f * logoMultiplier));
        CreateLogoImage(parent, logoSprite, "MINU Logo", new Vector2(0.79f, anchorY), new Vector2(360f * logoMultiplier, 150f * logoMultiplier));
    }

    private Image CreateLogoImage(Transform parent, Sprite sprite, string name, Vector2 anchor, Vector2 size)
    {
        if (sprite == null)
        {
            return null;
        }

        GameObject logoObject = new GameObject(name);
        logoObject.transform.SetParent(parent, false);

        RectTransform rectTransform = logoObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = size;

        Image image = logoObject.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private void CreateQuestionScreen(Transform parent)
    {
        CreateQuestionBackgroundCog(parent);
        CreateQuestionHeaderLogos(parent);
        CreateHomeButton(parent);

        progressLabel = CreateText(parent, "Progress", string.Empty, 50, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.765f), new Vector2(460f, 58f), AccentColor);

        questionPanel = CreateMillionairePanel(parent, "Question Panel", new Vector2(0.5f, 0.565f), new Vector2(1640f, 356f), questionScreenFieldSprite);
        questionLabel = CreateText(questionPanel, "Question", string.Empty, 60, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(1460f, 282f), FrameTextColor);
        questionLabel.resizeTextForBestFit = true;
        questionLabel.resizeTextMinSize = 14;
        questionLabel.resizeTextMaxSize = 60;

        answerButtons = new Button[AnswersCount];
        answerLabels = new Text[AnswersCount];

        Vector2[] positions =
        {
            new Vector2(0.27f, 0.315f),
            new Vector2(0.73f, 0.315f),
            new Vector2(0.27f, 0.105f),
            new Vector2(0.73f, 0.105f)
        };

        for (int i = 0; i < AnswersCount; i++)
        {
            int answerIndex = i;
            Button button = CreateMillionaireAnswerButton(parent, "Answer " + (i + 1), positions[i], new Vector2(800f, 198f),
                () => SelectAnswer(answerIndex));
            Text label = button.GetComponentInChildren<Text>();
            label.alignment = TextAnchor.MiddleCenter;
            label.fontSize = 40;
            label.resizeTextForBestFit = false;
            label.resizeTextMinSize = 40;
            label.resizeTextMaxSize = 40;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;

            answerButtons[i] = button;
            answerLabels[i] = label;
        }
    }

    private void CreateQuestionBackgroundCog(Transform parent)
    {
        if (questionBackgroundCogSprite == null)
        {
            return;
        }

        GameObject cogObject = new GameObject("Question Background Cog");
        cogObject.transform.SetParent(parent, false);

        questionBackgroundCog = cogObject.AddComponent<RectTransform>();
        questionBackgroundCog.anchorMin = new Vector2(0.5f, 0.5f);
        questionBackgroundCog.anchorMax = new Vector2(0.5f, 0.5f);
        questionBackgroundCog.pivot = new Vector2(0.5f, 0.5f);
        questionBackgroundCog.anchoredPosition = Vector2.zero;
        questionBackgroundCog.sizeDelta = new Vector2(1180f, 1180f);

        Image image = cogObject.AddComponent<Image>();
        image.sprite = questionBackgroundCogSprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.color = new Color(1f, 1f, 1f, 0.4f);

        questionBackgroundCog.SetAsFirstSibling();
    }

    private void CreateFeedbackScreen(Transform parent)
    {
        CreateHomeButton(parent);
        CreateFeedbackConfetti(parent);

        RectTransform panel = CreateMillionairePanel(parent, "Feedback Panel", new Vector2(0.5f, 0.53f), new Vector2(1640f, 463f), millionaireQuestionSprite);
        feedbackPanelCanvasGroup = panel.gameObject.AddComponent<CanvasGroup>();

        GameObject shimmerMaskObject = new GameObject("Feedback Shimmer Mask");
        shimmerMaskObject.transform.SetParent(panel, false);
        RectTransform shimmerMaskRect = shimmerMaskObject.AddComponent<RectTransform>();
        shimmerMaskRect.anchorMin = Vector2.zero;
        shimmerMaskRect.anchorMax = Vector2.one;
        shimmerMaskRect.offsetMin = Vector2.zero;
        shimmerMaskRect.offsetMax = Vector2.zero;

        Image shimmerMaskImage = shimmerMaskObject.AddComponent<Image>();
        shimmerMaskImage.sprite = feedbackShimmerMaskSprite;
        shimmerMaskImage.type = Image.Type.Simple;
        shimmerMaskImage.color = Color.white;
        shimmerMaskImage.material = CreateUiAlphaClipMaterial();
        shimmerMaskImage.raycastTarget = false;

        Mask shimmerMask = shimmerMaskObject.AddComponent<Mask>();
        shimmerMask.showMaskGraphic = false;

        GameObject shimmerObject = new GameObject("Correct Answer Gold Shimmer");
        shimmerObject.transform.SetParent(shimmerMaskRect, false);
        correctFeedbackShimmer = shimmerObject.AddComponent<RectTransform>();
        correctFeedbackShimmer.anchorMin = new Vector2(0.5f, 0.5f);
        correctFeedbackShimmer.anchorMax = new Vector2(0.5f, 0.5f);
        correctFeedbackShimmer.pivot = new Vector2(0.5f, 0.5f);
        correctFeedbackShimmer.sizeDelta = new Vector2(460f, 520f);
        correctFeedbackShimmer.localEulerAngles = new Vector3(0f, 0f, -14f);

        Image shimmerImage = shimmerObject.AddComponent<Image>();
        shimmerImage.sprite = goldShimmerSprite;
        shimmerImage.type = Image.Type.Simple;
        shimmerImage.raycastTarget = false;
        shimmerObject.SetActive(false);

        feedbackTitleLabel = CreateText(panel, "Feedback Title", string.Empty, 120, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(1360f, 220f), FrameTextColor);
    }

    private void CreateFeedbackConfetti(Transform parent)
    {
        if (superGameConfettiFrames == null || superGameConfettiFrames.Length == 0)
        {
            return;
        }

        GameObject confettiObject = new GameObject("Feedback Confetti");
        confettiObject.transform.SetParent(parent, false);

        RectTransform rectTransform = confettiObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        feedbackConfettiImage = confettiObject.AddComponent<RawImage>();
        feedbackConfettiImage.texture = superGameConfettiFrames[0];
        feedbackConfettiImage.color = Color.white;
        feedbackConfettiImage.raycastTarget = false;
        confettiObject.SetActive(false);
    }

    private RectTransform CreateMillionairePanel(Transform parent, string name, Vector2 anchor, Vector2 size, Sprite sprite)
    {
        GameObject panelObject = new GameObject(name);
        panelObject.transform.SetParent(parent, false);

        RectTransform rectTransform = panelObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = size;

        Image image = panelObject.AddComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.color = PanelColor;
        image.raycastTarget = false;

        return rectTransform;
    }

    private Button CreateMillionaireAnswerButton(Transform parent, string name, Vector2 anchor, Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);

        RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = size;

        Image image = buttonObject.AddComponent<Image>();
        image.sprite = millionaireAnswerSprite;
        image.type = Image.Type.Simple;
        image.color = Color.white;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);
        button.colors = CreateButtonColors(ButtonColor);

        Text text = CreateText(rectTransform, "Label", string.Empty, 28, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(size.x - 72f, size.y - 18f), FrameTextColor);
        text.raycastTarget = false;

        return button;
    }

    private void CreateFinalScreen(Transform parent)
    {
        CreateHomeButton(parent);

        CreateText(parent, "Final Title", "Викторина завершена", 76, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.9f), new Vector2(1300f, 110f), AccentColor);

        CreateQrPlaceholder(parent);

        finalScoreLabel = CreateText(parent, "Final Score", string.Empty, 50, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.39f), new Vector2(1500f, 100f), TextColor);

        finalRewardLabel = CreateText(parent, "Final Reward Message", string.Empty, 50, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.26f), new Vector2(1640f, 250f), TextColor);
        finalRewardLabel.resizeTextForBestFit = true;
        finalRewardLabel.resizeTextMinSize = 34;
        finalRewardLabel.resizeTextMaxSize = 50;

        Button restartButton = CreateMillionaireAnswerButton(parent, "Restart Button", new Vector2(0.5f, 0.10f), new Vector2(620f, 135f), () =>
        {
            PlaySound(clickSoundClip);
            ShowStartScreen();
        });
        Text restartButtonLabel = restartButton.GetComponentInChildren<Text>();
        restartButtonLabel.text = "В главное меню";
        restartButtonLabel.fontSize = 60;
    }

    private void CreateHomeButton(Transform parent)
    {
        Button button = CreateButton(parent, "Home Button", "⌂", new Vector2(0.95f, 0.92f), new Vector2(86f, 72f), () =>
        {
            PlaySound(clickSoundClip);
            ShowHomeConfirmation();
        });
        Text label = button.GetComponentInChildren<Text>();
        label.fontSize = 38;
        label.resizeTextForBestFit = false;
        button.interactable = false;
        button.gameObject.SetActive(false);
    }

    private void CreateHomeConfirmationOverlay(Transform parent)
    {
        Image dimmer = parent.gameObject.AddComponent<Image>();
        dimmer.color = new Color(0f, 0f, 0f, 0.42f);

        RectTransform panel = CreatePanel(parent, "Home Confirmation Panel", new Vector2(0.5f, 0.5f), new Vector2(940f, 460f), PanelColor);

        Text title = CreateText(panel, "Home Confirmation Title", "Вернуться на главный экран?", 56, FontStyle.Bold,
            TextAnchor.MiddleCenter, new Vector2(0.5f, 0.68f), new Vector2(820f, 120f), FrameTextColor);
        title.resizeTextForBestFit = true;
        title.resizeTextMinSize = 44;
        title.resizeTextMaxSize = 56;

        CreateText(panel, "Home Confirmation Text", "Текущая игра будет прервана.", 38, FontStyle.Normal,
            TextAnchor.MiddleCenter, new Vector2(0.5f, 0.48f), new Vector2(780f, 70f), FrameTextColor);

        Button confirmButton = CreateButton(panel, "Confirm Home Button", "Да", new Vector2(0.32f, 0.21f), new Vector2(280f, 104f), () =>
        {
            PlaySound(clickSoundClip);
            ConfirmReturnHome();
        });
        confirmButton.GetComponentInChildren<Text>().fontSize = 50;

        Button cancelButton = CreateButton(panel, "Cancel Home Button", "Нет", new Vector2(0.68f, 0.21f), new Vector2(280f, 104f), () =>
        {
            PlaySound(clickSoundClip);
            HideHomeConfirmation();
        });
        cancelButton.GetComponentInChildren<Text>().fontSize = 50;
    }

    private void CreateQrPlaceholder(Transform parent)
    {
        RectTransform qrFrame = CreatePanel(parent, "QR Code Placeholder", new Vector2(0.5f, 0.61f), new Vector2(310f, 310f), QrLightColor);

        CreateText(qrFrame, "QR Label", "QR", 44, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(120f, 70f), QrDarkColor).raycastTarget = false;

        CreateQrFinder(qrFrame, new Vector2(0.18f, 0.82f));
        CreateQrFinder(qrFrame, new Vector2(0.82f, 0.82f));
        CreateQrFinder(qrFrame, new Vector2(0.18f, 0.18f));

        Vector2[] modules =
        {
            new Vector2(0.38f, 0.78f), new Vector2(0.48f, 0.78f), new Vector2(0.62f, 0.72f),
            new Vector2(0.42f, 0.64f), new Vector2(0.56f, 0.62f), new Vector2(0.74f, 0.58f),
            new Vector2(0.36f, 0.48f), new Vector2(0.52f, 0.46f), new Vector2(0.66f, 0.44f),
            new Vector2(0.80f, 0.38f), new Vector2(0.42f, 0.30f), new Vector2(0.58f, 0.26f),
            new Vector2(0.72f, 0.22f)
        };

        for (int i = 0; i < modules.Length; i++)
        {
            CreateQrModule(qrFrame, modules[i], new Vector2(26f, 26f));
        }
    }

    private void CreateQrFinder(Transform parent, Vector2 anchor)
    {
        RectTransform outer = CreatePanel(parent, "QR Finder", anchor, new Vector2(84f, 84f), QrDarkColor, false);
        CreatePanel(outer, "QR Finder Inner", new Vector2(0.5f, 0.5f), new Vector2(54f, 54f), QrLightColor, false);
        CreatePanel(outer, "QR Finder Core", new Vector2(0.5f, 0.5f), new Vector2(30f, 30f), QrDarkColor, false);
    }

    private void CreateQrModule(Transform parent, Vector2 anchor, Vector2 size)
    {
        CreatePanel(parent, "QR Module", anchor, size, QrDarkColor, false);
    }

    private void StartQuiz()
    {
        currentQuestionIndex = GetDebugStartQuestionIndex();
        correctAnswers = 0;
        superGameActive = false;
        superGamePlayed = false;
        gameSessionActive = true;
        SendOscCommand(OscGameStartAddress);
        ShowQuestion();
    }

    private int GetDebugStartQuestionIndex()
    {
        if (string.IsNullOrWhiteSpace(DebugStartQuestion))
        {
            return 0;
        }

        if (int.TryParse(DebugStartQuestion, out int questionNumber)
            && questionNumber >= 1
            && questionNumber <= questions.Length)
        {
            Debug.Log("Debug: викторина начинается с вопроса " + questionNumber + ".");
            return questionNumber - 1;
        }

        Debug.LogWarning("Debug Start Question должен содержать номер от 1 до " + questions.Length + ". Викторина начнется с первого вопроса.");
        return 0;
    }

    private void StartCountdown()
    {
        if (countdownRoutine == null)
        {
            PlaySound(countdownSoundClip);
            countdownRoutine = StartCoroutine(ShowStartCountdown());
        }
    }

    private IEnumerator ShowStartCountdown()
    {
        startButton.gameObject.SetActive(false);

        for (int i = 0; i < countdownLogoSprites.Length; i++)
        {
            if (countdownLogoSprites[i] != null)
            {
                superLogoImage.sprite = countdownLogoSprites[i];
            }

            yield return new WaitForSeconds(1f);
        }

        superLogoImage.sprite = superLogoSprite;
        countdownRoutine = null;
        StartQuiz();
    }

    private void ShowQuestion()
    {
        answerLocked = false;
        PrepareQuestionScreenFadeIn();
        SetActiveScreen(questionScreen);

        QuestionData question = GetCurrentQuestion();
        progressLabel.text = superGameActive ? "Суперигра" : "Вопрос " + (currentQuestionIndex + 1) + " из " + questions.Length;
        progressLabel.fontSize = superGameActive ? 52 : 50;
        Vector2 progressAnchor = superGameActive ? new Vector2(0.5f, 0.775f) : new Vector2(0.5f, 0.765f);
        progressLabel.rectTransform.anchorMin = progressAnchor;
        progressLabel.rectTransform.anchorMax = progressAnchor;
        questionLabel.text = question.Text;
        questionLabel.fontSize = superGameActive ? 44 : 60;
        questionLabel.resizeTextMaxSize = superGameActive ? 44 : 60;
        Vector2 questionAnchor = superGameActive ? new Vector2(0.5f, 0.545f) : new Vector2(0.5f, 0.565f);
        questionPanel.anchorMin = questionAnchor;
        questionPanel.anchorMax = questionAnchor;
        questionPanel.sizeDelta = superGameActive ? new Vector2(1804f, 436f) : new Vector2(1640f, 356f);
        questionLabel.rectTransform.sizeDelta = superGameActive ? new Vector2(1606f, 361f) : new Vector2(1460f, 282f);

        Vector2[] answerAnchors = superGameActive
            ? new[]
            {
                new Vector2(0.27f, 0.27f),
                new Vector2(0.73f, 0.27f),
                new Vector2(0.27f, 0.095f),
                new Vector2(0.73f, 0.095f)
            }
            : new[]
            {
                new Vector2(0.27f, 0.305f),
                new Vector2(0.73f, 0.305f),
                new Vector2(0.27f, 0.105f),
                new Vector2(0.73f, 0.105f)
            };

        for (int i = 0; i < answerButtons.Length; i++)
        {
            RectTransform answerTransform = answerButtons[i].GetComponent<RectTransform>();
            answerTransform.anchorMin = answerAnchors[i];
            answerTransform.anchorMax = answerAnchors[i];
            answerTransform.sizeDelta = superGameActive ? new Vector2(792f, 187f) : new Vector2(800f, 198f);
            answerButtons[i].interactable = true;
            answerLabels[i].text = question.Answers[i];
            SetButtonColor(answerButtons[i], ButtonColor);
        }

        questionScreenFadeRoutine = StartCoroutine(FadeInQuestionScreen());
    }

    private void PrepareQuestionScreenFadeIn()
    {
        if (questionScreenFadeRoutine != null)
        {
            StopCoroutine(questionScreenFadeRoutine);
            questionScreenFadeRoutine = null;
        }

        if (questionScreenCanvasGroup == null)
        {
            return;
        }

        questionScreenCanvasGroup.alpha = 0f;
        questionScreenCanvasGroup.interactable = false;
        questionScreenCanvasGroup.blocksRaycasts = false;
    }

    private IEnumerator FadeInQuestionScreen()
    {
        const float delay = 0.3f;
        const float duration = 1.5f;
        float elapsed = 0f;

        yield return new WaitForSeconds(delay);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float smoothProgress = progress * progress * (3f - 2f * progress);
            questionScreenCanvasGroup.alpha = smoothProgress;
            yield return null;
        }

        questionScreenCanvasGroup.alpha = 1f;
        questionScreenCanvasGroup.interactable = true;
        questionScreenCanvasGroup.blocksRaycasts = true;
        questionScreenFadeRoutine = null;
    }

    private void SelectAnswer(int answerIndex)
    {
        if (answerLocked)
        {
            return;
        }

        PlaySound(clickSoundClip);
        answerLocked = true;
        QuestionData question = GetCurrentQuestion();
        bool isCorrect = answerIndex == question.CorrectAnswerIndex;
        selectedAnswerWasCorrect = isCorrect;

        if (superGameActive)
        {
            superGamePlayed = true;
        }

        if (!superGameActive && isCorrect)
        {
            correctAnswers++;
        }

        selectedAnswerIndex = answerIndex;

        for (int i = 0; i < answerButtons.Length; i++)
        {
            answerButtons[i].interactable = false;
        }

        feedbackTitleLabel.text = isCorrect ? "Правильно!" : "Неправильно";
        feedbackTitleLabel.color = FrameTextColor;

        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
        }

        feedbackRoutine = StartCoroutine(ShowFeedbackThenContinue());
    }

    private IEnumerator ShowFeedbackThenContinue()
    {
        const int blinkCount = 5;
        const float blinkInterval = 0.3f;
        const float feedbackDuration = 5f;

        for (int i = 0; i < blinkCount; i++)
        {
            PlaySound(clickSoundClip);
            SetButtonColor(answerButtons[selectedAnswerIndex], SelectedAnswerBlinkColor);
            yield return new WaitForSeconds(blinkInterval);
            SetButtonColor(answerButtons[selectedAnswerIndex], ButtonColor);
            yield return new WaitForSeconds(blinkInterval);
        }

        correctFeedbackShimmer.gameObject.SetActive(false);
        feedbackPanelCanvasGroup.alpha = 0f;
        feedbackTitleLabel.rectTransform.anchoredPosition = Vector2.zero;
        feedbackTitleLabel.rectTransform.localEulerAngles = Vector3.zero;
        feedbackTitleLabel.color = selectedAnswerWasCorrect
            ? new Color(FrameTextColor.r, FrameTextColor.g, FrameTextColor.b, 0f)
            : FrameTextColor;
        ResetFeedbackConfetti();
        SetActiveScreen(feedbackScreen);
        SendOscCommand(selectedAnswerWasCorrect ? OscAnswerCorrectAddress : OscAnswerWrongAddress);

        if (selectedAnswerWasCorrect)
        {
            StartCoroutine(PlayCorrectFeedbackAnimation());
        }
        else
        {
            StartCoroutine(PlayIncorrectFeedbackTitleDrop());
        }

        float feedbackElapsed = 0f;
        while (feedbackElapsed < feedbackDuration)
        {
            feedbackElapsed += Time.deltaTime;
            yield return null;
        }

        HideFeedbackConfetti();
        feedbackRoutine = null;

        if (superGameActive)
        {
            ShowFinalScreen();
            yield break;
        }

        currentQuestionIndex++;

        if (currentQuestionIndex < questions.Length)
        {
            ShowQuestion();
            yield break;
        }

        if (correctAnswers >= 4 || DebugForceSuperGameAccess)
        {
            ShowSuperGame();
            yield break;
        }

        ShowFinalScreen();
    }

    private IEnumerator PlayCorrectFeedbackAnimation()
    {
        const float fadeDuration = 1.2f;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / fadeDuration);
            float smoothProgress = progress * progress * (3f - 2f * progress);
            FadeInFeedbackPanel(smoothProgress);
            feedbackTitleLabel.color = new Color(
                FrameTextColor.r,
                FrameTextColor.g,
                FrameTextColor.b,
                smoothProgress);
            yield return null;
        }

        FadeInFeedbackPanel(1f);
        feedbackTitleLabel.color = FrameTextColor;
        PlayCorrectSound();

        if (superGameActive && selectedAnswerWasCorrect)
        {
            StartCoroutine(PlayFeedbackConfettiOnce());
        }

        yield return PlayCorrectFeedbackShimmer();
    }

    private void FadeInFeedbackPanel(float progress)
    {
        feedbackPanelCanvasGroup.alpha = Mathf.Clamp01(progress);
    }

    private IEnumerator PlayCorrectFeedbackShimmer()
    {
        const float duration = 3f;
        const float startX = -1040f;
        const float endX = 1040f;

        correctFeedbackShimmer.anchoredPosition = new Vector2(startX, 0f);
        correctFeedbackShimmer.gameObject.SetActive(true);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float smoothProgress = progress * progress * (3f - 2f * progress);
            correctFeedbackShimmer.anchoredPosition = new Vector2(Mathf.Lerp(startX, endX, smoothProgress), 0f);
            yield return null;
        }

        correctFeedbackShimmer.gameObject.SetActive(false);
    }

    private IEnumerator PlayIncorrectFeedbackTitleDrop()
    {
        RectTransform titleTransform = feedbackTitleLabel.rectTransform;

        yield return AnimateFeedbackTitlePoseAndFadeIn(
            titleTransform,
            new Vector2(0f, 650f),
            0f,
            new Vector2(0f, 0f),
            0f,
            1.5f,
            true);

        PlayIncorrectSound();

        yield return AnimateFeedbackTitlePose(
            titleTransform,
            Vector2.zero,
            0f,
            new Vector2(0f, -18f),
            16f,
            0.28f,
            false);

        yield return AnimateFeedbackTitlePose(
            titleTransform,
            new Vector2(0f, -18f),
            16f,
            new Vector2(0f, 10f),
            -9f,
            0.38f,
            false);

        yield return AnimateFeedbackTitlePose(
            titleTransform,
            new Vector2(0f, 10f),
            -9f,
            Vector2.zero,
            0f,
            0.5f,
            false);
    }

    private IEnumerator AnimateFeedbackTitlePoseAndFadeIn(
        RectTransform titleTransform,
        Vector2 startPosition,
        float startAngle,
        Vector2 endPosition,
        float endAngle,
        float duration,
        bool easeIn)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = easeIn
                ? progress * progress * progress
                : progress * progress * (3f - 2f * progress);

            FadeInFeedbackPanel(progress * progress * (3f - 2f * progress));
            titleTransform.anchoredPosition = Vector2.Lerp(startPosition, endPosition, easedProgress);
            titleTransform.localEulerAngles = new Vector3(0f, 0f, Mathf.LerpAngle(startAngle, endAngle, easedProgress));
            yield return null;
        }

        FadeInFeedbackPanel(1f);
        titleTransform.anchoredPosition = endPosition;
        titleTransform.localEulerAngles = new Vector3(0f, 0f, endAngle);
    }

    private IEnumerator AnimateFeedbackTitlePose(
        RectTransform titleTransform,
        Vector2 startPosition,
        float startAngle,
        Vector2 endPosition,
        float endAngle,
        float duration,
        bool easeIn)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = easeIn
                ? progress * progress * progress
                : progress * progress * (3f - 2f * progress);

            titleTransform.anchoredPosition = Vector2.Lerp(startPosition, endPosition, easedProgress);
            titleTransform.localEulerAngles = new Vector3(0f, 0f, Mathf.LerpAngle(startAngle, endAngle, easedProgress));
            yield return null;
        }

        titleTransform.anchoredPosition = endPosition;
        titleTransform.localEulerAngles = new Vector3(0f, 0f, endAngle);
    }

    private void ShowStartScreen()
    {
        HideHomeConfirmation();

        if (superGameIntroRoutine != null)
        {
            StopCoroutine(superGameIntroRoutine);
            superGameIntroRoutine = null;
        }

        if (countdownRoutine != null)
        {
            StopCoroutine(countdownRoutine);
            countdownRoutine = null;
        }

        superLogoImage.sprite = superLogoSprite;
        startButton.gameObject.SetActive(true);
        SetActiveScreen(startScreen);
    }

    private void ShowHomeConfirmation()
    {
        feedbackWasInterruptedByHomeConfirmation = feedbackRoutine != null;

        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
            feedbackRoutine = null;
        }

        homeConfirmationOverlay.SetActive(true);
    }

    private void HideHomeConfirmation()
    {
        if (homeConfirmationOverlay == null)
        {
            return;
        }

        homeConfirmationOverlay.SetActive(false);

        if (feedbackWasInterruptedByHomeConfirmation)
        {
            feedbackWasInterruptedByHomeConfirmation = false;
            feedbackRoutine = StartCoroutine(ShowFeedbackThenContinue());
        }
    }

    private void ConfirmReturnHome()
    {
        feedbackWasInterruptedByHomeConfirmation = false;

        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
            feedbackRoutine = null;
        }

        homeConfirmationOverlay.SetActive(false);
        superGameActive = false;
        answerLocked = false;
        SendGameEndOscIfNeeded();
        ShowStartScreen();
    }

    private void ShowFinalScreen()
    {
        superGameActive = false;
        SendGameEndOscIfNeeded();
        PrepareFinalScreenFadeIn();
        finalScoreLabel.text = "Ваш результат в основной игре: " + correctAnswers + " из " + questions.Length;
        finalScoreLabel.color = AccentColor;
        //superGamePlayed = true;
        finalRewardLabel.text = superGamePlayed
            ? "Поздравляем!!! В качестве приза получите ЛЮБОЙ напиток в нашем баре. Сфотографируйте QR-код и предъявите бармену."
            : "Попробуйте сыграть еще раз. В качестве поощрения за участие Вам предлагается приз в виде БЕЗАЛКОГОЛЬНОГО напитка в нашем баре. Сфотографируйте QR-код и предъявите бармену.";
        finalRewardLabel.color = AccentColor;
        SetActiveScreen(finalScreen);
        PlaySound(finalScreenSoundClip);
        finalScreenFadeRoutine = StartCoroutine(FadeInFinalScreen());
    }

    private void PrepareFinalScreenFadeIn()
    {
        if (finalScreenFadeRoutine != null)
        {
            StopCoroutine(finalScreenFadeRoutine);
            finalScreenFadeRoutine = null;
        }

        if (finalScreenCanvasGroup == null)
        {
            return;
        }

        finalScreenCanvasGroup.alpha = 0f;
        finalScreenCanvasGroup.interactable = false;
        finalScreenCanvasGroup.blocksRaycasts = false;
    }

    private IEnumerator FadeInFinalScreen()
    {
        const float delay = 0.3f;
        const float duration = 1.5f;
        float elapsed = 0f;

        yield return new WaitForSeconds(delay);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float smoothProgress = progress * progress * (3f - 2f * progress);
            finalScreenCanvasGroup.alpha = smoothProgress;
            yield return null;
        }

        finalScreenCanvasGroup.alpha = 1f;
        finalScreenCanvasGroup.interactable = true;
        finalScreenCanvasGroup.blocksRaycasts = true;
        finalScreenFadeRoutine = null;
    }

    private void ShowSuperGame()
    {
        superGameActive = true;
        superGameIntroLogoImage.color = new Color(1f, 1f, 1f, 0f);
        ResetSuperGameConfetti();
        SetActiveScreen(superGameIntroScreen);
        superGameIntroRoutine = StartCoroutine(ShowSuperGameAfterIntro());
    }

    private IEnumerator ShowSuperGameAfterIntro()
    {
        const float fadeDuration = 3f;
        const float confettiStartDelay = 1.25f;
        const float finishPause = 0.75f;
        float elapsed = 0f;
        bool confettiStarted = false;
        bool confettiFinished = false;

        PlayFinalRoundSound();

        while (elapsed < fadeDuration || !confettiFinished)
        {
            float deltaTime = Time.deltaTime;
            elapsed += deltaTime;

            if (!confettiStarted && elapsed >= confettiStartDelay)
            {
                ShowSuperGameConfetti();
                confettiStarted = true;
            }

            if (confettiStarted && !confettiFinished)
            {
                confettiFinished = AdvanceSuperGameConfetti(deltaTime);
            }

            float progress = Mathf.Clamp01(elapsed / fadeDuration);
            float smoothProgress = progress * progress * (3f - 2f * progress);
            superGameIntroLogoImage.color = new Color(1f, 1f, 1f, smoothProgress);
            yield return null;
        }

        superGameIntroLogoImage.color = Color.white;
        HideSuperGameConfetti();
        yield return new WaitForSeconds(finishPause);
        superGameIntroRoutine = null;
        ShowQuestion();
    }

    private void ResetSuperGameConfetti()
    {
        superGameConfettiFrameIndex = 0;
        superGameConfettiFrameTimer = 0f;

        if (superGameConfettiImage == null || superGameConfettiFrames == null || superGameConfettiFrames.Length == 0)
        {
            return;
        }

        superGameConfettiImage.texture = superGameConfettiFrames[0];
        superGameConfettiImage.gameObject.SetActive(false);
    }

    private void ShowSuperGameConfetti()
    {
        if (superGameConfettiImage == null || superGameConfettiFrames == null || superGameConfettiFrames.Length == 0)
        {
            return;
        }

        superGameConfettiImage.gameObject.SetActive(true);
    }

    private void HideSuperGameConfetti()
    {
        if (superGameConfettiImage == null)
        {
            return;
        }

        superGameConfettiImage.gameObject.SetActive(false);
    }

    private void ResetFeedbackConfetti()
    {
        feedbackConfettiFrameIndex = 0;
        feedbackConfettiFrameTimer = 0f;

        if (feedbackConfettiImage == null || superGameConfettiFrames == null || superGameConfettiFrames.Length == 0)
        {
            return;
        }

        feedbackConfettiImage.texture = superGameConfettiFrames[0];
        feedbackConfettiImage.gameObject.SetActive(false);
    }

    private void ShowFeedbackConfetti()
    {
        if (feedbackConfettiImage == null || superGameConfettiFrames == null || superGameConfettiFrames.Length == 0)
        {
            return;
        }

        feedbackConfettiImage.gameObject.SetActive(true);
    }

    private void HideFeedbackConfetti()
    {
        if (feedbackConfettiImage == null)
        {
            return;
        }

        feedbackConfettiImage.gameObject.SetActive(false);
    }

    private IEnumerator PlayFeedbackConfettiOnce()
    {
        yield return new WaitForSeconds(0.5f);
        ShowFeedbackConfetti();

        bool confettiFinished = false;
        while (!confettiFinished)
        {
            confettiFinished = AdvanceFeedbackConfetti(Time.deltaTime);
            yield return null;
        }

        HideFeedbackConfetti();
    }

    private bool AdvanceSuperGameConfetti(float deltaTime)
    {
        if (superGameConfettiImage == null || !superGameConfettiImage.gameObject.activeSelf ||
            superGameConfettiFrames == null || superGameConfettiFrames.Length == 0)
        {
            return true;
        }

        if (superGameConfettiFrameIndex >= superGameConfettiFrames.Length - 1)
        {
            return true;
        }

        superGameConfettiFrameTimer += deltaTime;
        float frameDuration = GetSuperGameConfettiFrameDuration(superGameConfettiFrameIndex);

        while (superGameConfettiFrameTimer >= frameDuration)
        {
            superGameConfettiFrameTimer -= frameDuration;
            superGameConfettiFrameIndex++;
            superGameConfettiImage.texture = superGameConfettiFrames[superGameConfettiFrameIndex];

            if (superGameConfettiFrameIndex >= superGameConfettiFrames.Length - 1)
            {
                return true;
            }

            frameDuration = GetSuperGameConfettiFrameDuration(superGameConfettiFrameIndex);
        }

        return false;
    }

    private bool AdvanceFeedbackConfetti(float deltaTime)
    {
        if (feedbackConfettiImage == null || !feedbackConfettiImage.gameObject.activeSelf ||
            superGameConfettiFrames == null || superGameConfettiFrames.Length == 0)
        {
            return true;
        }

        if (feedbackConfettiFrameIndex >= superGameConfettiFrames.Length - 1)
        {
            return true;
        }

        feedbackConfettiFrameTimer += deltaTime;
        float frameDuration = GetSuperGameConfettiFrameDuration(feedbackConfettiFrameIndex);

        while (feedbackConfettiFrameTimer >= frameDuration)
        {
            feedbackConfettiFrameTimer -= frameDuration;
            feedbackConfettiFrameIndex++;
            feedbackConfettiImage.texture = superGameConfettiFrames[feedbackConfettiFrameIndex];

            if (feedbackConfettiFrameIndex >= superGameConfettiFrames.Length - 1)
            {
                return true;
            }

            frameDuration = GetSuperGameConfettiFrameDuration(feedbackConfettiFrameIndex);
        }

        return false;
    }

    private float GetSuperGameConfettiFrameDuration(int frameIndex)
    {
        if (superGameConfettiFrameDurations == null || frameIndex < 0 || frameIndex >= superGameConfettiFrameDurations.Length)
        {
            return 0.04f;
        }

        return Mathf.Max(0.01f, superGameConfettiFrameDurations[frameIndex]);
    }

    private QuestionData GetCurrentQuestion()
    {
        return superGameActive ? superQuestion : questions[currentQuestionIndex];
    }

    private GameObject CreateScreen(Transform parent, string name)
    {
        GameObject screen = new GameObject(name);
        screen.transform.SetParent(parent, false);

        RectTransform rectTransform = screen.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        return screen;
    }

    private RectTransform CreatePanel(Transform parent, string name, Vector2 anchor, Vector2 size, Color color, bool rounded = true)
    {
        GameObject panelObject = new GameObject(name);
        panelObject.transform.SetParent(parent, false);

        RectTransform rectTransform = panelObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = size;

        Image image = panelObject.AddComponent<Image>();
        if (rounded)
        {
            image.sprite = roundedRectSprite;
            image.type = Image.Type.Sliced;
        }

        image.color = color;

        return rectTransform;
    }

    private Button CreateButton(Transform parent, string name, string label, Vector2 anchor, Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);

        RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = size;

        Image image = buttonObject.AddComponent<Image>();
        image.sprite = roundedRectSprite;
        image.type = Image.Type.Sliced;
        image.color = Color.white;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);
        button.colors = CreateButtonColors(ButtonColor);

        Text text = CreateText(rectTransform, "Label", label, 36, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), size, Color.white);
        text.raycastTarget = false;
        text.color = GetReadableTextColor(ButtonColor);

        return button;
    }

    private InputField CreateInputField(Transform parent, string name, string initialValue, Vector2 anchor, Vector2 size, InputField.ContentType contentType)
    {
        GameObject inputObject = new GameObject(name);
        inputObject.transform.SetParent(parent, false);

        RectTransform rectTransform = inputObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = size;

        Image image = inputObject.AddComponent<Image>();
        image.sprite = roundedRectSprite;
        image.type = Image.Type.Sliced;
        image.color = Color.white;

        InputField inputField = inputObject.AddComponent<InputField>();
        inputField.targetGraphic = image;
        inputField.contentType = contentType;
        inputField.text = initialValue;
        inputField.caretColor = FrameTextColor;
        inputField.selectionColor = new Color(FrameTextColor.r, FrameTextColor.g, FrameTextColor.b, 0.28f);

        Text text = CreateText(rectTransform, "Text", string.Empty, 28, FontStyle.Normal, TextAnchor.MiddleLeft,
            new Vector2(0.5f, 0.5f), new Vector2(size.x - 32f, size.y - 8f), FrameTextColor);
        text.raycastTarget = false;

        Text placeholder = CreateText(rectTransform, "Placeholder", initialValue, 28, FontStyle.Normal, TextAnchor.MiddleLeft,
            new Vector2(0.5f, 0.5f), new Vector2(size.x - 32f, size.y - 8f), new Color(FrameTextColor.r, FrameTextColor.g, FrameTextColor.b, 0.45f));
        placeholder.raycastTarget = false;

        inputField.textComponent = text;
        inputField.placeholder = placeholder;
        inputField.text = initialValue;

        return inputField;
    }

    private Text CreateText(Transform parent, string name, string content, int fontSize, FontStyle style, TextAnchor alignment,
        Vector2 anchor, Vector2 size, Color color)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = size;

        Text text = textObject.AddComponent<Text>();
        text.text = content;
        text.font = GetFontForStyle(style);
        text.fontSize = fontSize;
        text.fontStyle = GetRuntimeFontStyle(style);
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        return text;
    }

    private ColorBlock CreateButtonColors(Color normalColor)
    {
        ColorBlock colors = ColorBlock.defaultColorBlock;
        colors.normalColor = normalColor;
        colors.highlightedColor = ButtonHoverColor;
        colors.pressedColor = AccentColor;
        colors.selectedColor = ButtonHoverColor;
        colors.disabledColor = normalColor;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        return colors;
    }

    private void SetButtonColor(Button button, Color color)
    {
        Image image = button.GetComponent<Image>();
        image.color = Color.white;
        button.colors = CreateButtonColors(color);

        Text label = button.GetComponentInChildren<Text>();
        if (label != null)
        {
            label.color = GetReadableTextColor(color);
        }
    }

    private Color GetReadableTextColor(Color backgroundColor)
    {
        float luminance = 0.2126f * backgroundColor.r + 0.7152f * backgroundColor.g + 0.0722f * backgroundColor.b;
        return luminance > 0.55f ? FrameTextColor : Color.white;
    }

    private void SetActiveScreen(GameObject activeScreen)
    {
        startScreen.SetActive(activeScreen == startScreen);
        questionScreen.SetActive(activeScreen == questionScreen);
        superGameIntroScreen.SetActive(activeScreen == superGameIntroScreen);
        feedbackScreen.SetActive(activeScreen == feedbackScreen);
        finalScreen.SetActive(activeScreen == finalScreen);
    }

    private void LoadOscSettings()
    {
        oscRemoteHost = PlayerPrefs.GetString(OscHostPrefsKey, DefaultOscHost);
        oscRemotePort = PlayerPrefs.GetInt(OscPortPrefsKey, DefaultOscPort);

        if (string.IsNullOrWhiteSpace(oscRemoteHost))
        {
            oscRemoteHost = DefaultOscHost;
        }

        if (oscRemotePort <= 0)
        {
            oscRemotePort = DefaultOscPort;
        }
    }

    private void CreateOscTransmitter()
    {
        oscTransmitter = gameObject.AddComponent<OSCTransmitter>();
        ApplyOscSettingsToTransmitter();
    }

    private void ToggleOscSettingsPanel()
    {
        if (oscSettingsPanel == null)
        {
            return;
        }

        oscSettingsPanel.SetActive(!oscSettingsPanel.activeSelf);
    }

    private void SaveOscSettingsFromInputs()
    {
        string host = oscHostInput != null ? oscHostInput.text.Trim() : oscRemoteHost;
        if (string.IsNullOrWhiteSpace(host))
        {
            host = DefaultOscHost;
        }

        int port = oscRemotePort;
        if (oscPortInput == null || !int.TryParse(oscPortInput.text.Trim(), out port) || port <= 0)
        {
            port = DefaultOscPort;
        }

        oscRemoteHost = host;
        oscRemotePort = port;

        if (oscHostInput != null)
        {
            oscHostInput.text = oscRemoteHost;
        }

        if (oscPortInput != null)
        {
            oscPortInput.text = oscRemotePort.ToString();
        }

        PlayerPrefs.SetString(OscHostPrefsKey, oscRemoteHost);
        PlayerPrefs.SetInt(OscPortPrefsKey, oscRemotePort);
        PlayerPrefs.Save();

        ApplyOscSettingsToTransmitter();

        if (oscSettingsPanel != null)
        {
            oscSettingsPanel.SetActive(false);
        }
    }

    private void ApplyOscSettingsToTransmitter()
    {
        if (oscTransmitter == null)
        {
            return;
        }

        oscTransmitter.RemoteHost = oscRemoteHost;
        oscTransmitter.RemotePort = oscRemotePort;
    }

    private void SendOscCommand(string address)
    {
        if (oscTransmitter == null || string.IsNullOrEmpty(address))
        {
            return;
        }

        try
        {
            oscTransmitter.Send(new OSCMessage(address));
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning("OSC command was not sent: " + address + ". " + exception.Message);
        }
    }

    private void SendGameEndOscIfNeeded()
    {
        if (!gameSessionActive)
        {
            return;
        }

        SendOscCommand(OscGameEndAddress);
        gameSessionActive = false;
    }

    private void LoadBrandAssets()
    {
        regularFont = Resources.Load<Font>("Brand/MYRIADPRO-REGULAR");
        boldFont = Resources.Load<Font>("Brand/MYRIADPRO-BOLD");

        if (regularFont == null)
        {
            regularFont = Font.CreateDynamicFontFromOSFont(new[] { "Myriad Pro", "Tahoma", "Arial" }, 24);
        }

        if (boldFont == null)
        {
            boldFont = Font.CreateDynamicFontFromOSFont(new[] { "Myriad Pro Bold", "Myriad Pro", "Tahoma", "Arial" }, 24);
        }

        if (regularFont == null)
        {
            regularFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        if (regularFont == null)
        {
            regularFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        if (boldFont == null)
        {
            boldFont = regularFont;
        }

        Texture2D logoTexture = Resources.Load<Texture2D>("Brand/logo_machines");
        if (logoTexture != null)
        {
            logoSprite = Sprite.Create(
                logoTexture,
                new Rect(0f, 0f, logoTexture.width, logoTexture.height),
                new Vector2(0.5f, 0.5f),
                100f);
        }

        Texture2D pmufLogoTexture = Resources.Load<Texture2D>("Brand/logo_PMUF");
        if (pmufLogoTexture != null)
        {
            pmufLogoSprite = Sprite.Create(
                pmufLogoTexture,
                new Rect(0f, 0f, pmufLogoTexture.width, pmufLogoTexture.height),
                new Vector2(0.5f, 0.5f),
                100f);
        }

        Texture2D minuLogoTexture = Resources.Load<Texture2D>("Brand/logo_MINU_gold");
        if (minuLogoTexture != null)
        {
            minuLogoSprite = Sprite.Create(
                minuLogoTexture,
                new Rect(0f, 0f, minuLogoTexture.width, minuLogoTexture.height),
                new Vector2(0.5f, 0.5f),
                100f);
        }

        Texture2D superLogoTexture = Resources.Load<Texture2D>("Brand/logo_metallic");
        if (superLogoTexture != null)
        {
            superLogoSprite = Sprite.Create(
                superLogoTexture,
                new Rect(0f, 0f, superLogoTexture.width, superLogoTexture.height),
                new Vector2(0.5f, 0.5f),
                100f);
        }

        superGameLogoSprite = LoadSpriteFromResources("Brand/logo_metallic_supergame");

        countdownLogoSprites = new[]
        {
            LoadSpriteFromResources("Brand/logo_metallic_3"),
            LoadSpriteFromResources("Brand/logo_metallic_2"),
            LoadSpriteFromResources("Brand/logo_metallic_1")
        };

        roundedRectSprite = CreateRoundedRectSprite();
        goldShimmerSprite = CreateGoldShimmerSprite();
        questionScreenFieldSprite = LoadSpriteFromResources("Brand/answer_feild_metallic");
        millionaireQuestionSprite = LoadSpriteFromResources("Brand/answer_feild_metallic");
        millionaireAnswerSprite = LoadSpriteFromResources("Brand/answer_feild_metallic");
        questionBackgroundCogSprite = LoadSpriteFromResources("Brand/spinning_cog");
        if (questionBackgroundCogSprite == null)
        {
            questionBackgroundCogSprite = LoadSpriteFromResources("Brand/spininng_cog");
        }

        feedbackShimmerMaskSprite = LoadSpriteFromResources("Brand/answer_feild_metallic_shimmer_mask");
        if (feedbackShimmerMaskSprite != null)
        {
            feedbackShimmerMaskSprite.texture.filterMode = FilterMode.Point;
            feedbackShimmerMaskSprite.texture.wrapMode = TextureWrapMode.Clamp;
        }

        countdownSoundClip = Resources.Load<AudioClip>("Sound/countdown");
        clickSoundClip = Resources.Load<AudioClip>("Sound/click");
        correctSoundClip = Resources.Load<AudioClip>("Sound/correct");
        incorrectSoundClip = Resources.Load<AudioClip>("Sound/incorrect");
        finalRoundSoundClip = Resources.Load<AudioClip>("Sound/final_round");
        finalScreenSoundClip = Resources.Load<AudioClip>("Sound/final_screen");
        LoadSuperGameConfettiFrames();
    }

    private void LoadSuperGameConfettiFrames()
    {
        superGameConfettiFrames = Resources.LoadAll<Texture2D>("Video/conf2_frames");
        if (superGameConfettiFrames == null || superGameConfettiFrames.Length == 0)
        {
            return;
        }

        System.Array.Sort(superGameConfettiFrames, (first, second) => string.CompareOrdinal(first.name, second.name));

        for (int i = 0; i < superGameConfettiFrames.Length; i++)
        {
            superGameConfettiFrames[i].wrapMode = TextureWrapMode.Clamp;
            superGameConfettiFrames[i].filterMode = FilterMode.Bilinear;
        }

        superGameConfettiFrameDurations = new float[superGameConfettiFrames.Length];
        for (int i = 0; i < superGameConfettiFrameDurations.Length; i++)
        {
            superGameConfettiFrameDurations[i] = 0.04f;
        }

        TextAsset delaysAsset = Resources.Load<TextAsset>("Video/conf2_delays");
        if (delaysAsset == null || string.IsNullOrWhiteSpace(delaysAsset.text))
        {
            return;
        }

        string[] delayValues = delaysAsset.text.Split(',');
        int delayCount = Mathf.Min(delayValues.Length, superGameConfettiFrameDurations.Length);
        for (int i = 0; i < delayCount; i++)
        {
            if (float.TryParse(delayValues[i], NumberStyles.Float, CultureInfo.InvariantCulture, out float duration))
            {
                superGameConfettiFrameDurations[i] = Mathf.Max(0.01f, duration);
            }
        }
    }

    private void CreateAudioSource()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;

        correctAudioSource = gameObject.AddComponent<AudioSource>();
        correctAudioSource.playOnAwake = false;
        correctAudioSource.loop = false;
        correctAudioSource.pitch = 0.8f;

        incorrectAudioSource = gameObject.AddComponent<AudioSource>();
        incorrectAudioSource.playOnAwake = false;
        incorrectAudioSource.loop = false;
        incorrectAudioSource.pitch = 0.7f;

        finalRoundAudioSource = gameObject.AddComponent<AudioSource>();
        finalRoundAudioSource.playOnAwake = false;
        finalRoundAudioSource.loop = false;
        finalRoundAudioSource.pitch = 1.0f;
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource == null || clip == null)
        {
            return;
        }

        audioSource.PlayOneShot(clip);
    }

    private void PlayCorrectSound()
    {
        if (correctAudioSource == null || correctSoundClip == null)
        {
            return;
        }

        correctAudioSource.PlayOneShot(correctSoundClip);
    }

    private void PlayIncorrectSound()
    {
        if (incorrectAudioSource == null || incorrectSoundClip == null)
        {
            return;
        }

        incorrectAudioSource.PlayOneShot(incorrectSoundClip);
    }

    private void PlayFinalRoundSound()
    {
        if (finalRoundAudioSource == null || finalRoundSoundClip == null)
        {
            return;
        }

        finalRoundAudioSource.PlayOneShot(finalRoundSoundClip);
    }

    private Font GetFontForStyle(FontStyle style)
    {
        return style == FontStyle.Bold ? boldFont : regularFont;
    }

    private FontStyle GetRuntimeFontStyle(FontStyle style)
    {
        return style == FontStyle.Bold && boldFont == regularFont ? FontStyle.Bold : FontStyle.Normal;
    }

    private Sprite LoadSpriteFromResources(string path)
    {
        Texture2D texture = Resources.Load<Texture2D>(path);

        if (texture == null)
        {
            Debug.LogWarning("Texture was not found in Resources: " + path);
            return null;
        }

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f);
    }

    private Sprite CreateRoundedRectSprite()
    {
        const int size = 64;
        const int radius = 14;

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "Rounded Rect Sprite";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float alpha = GetRoundedRectAlpha(x, y, size, radius);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(radius, radius, radius, radius));
    }

    private Sprite CreateGoldShimmerSprite()
    {
        const int width = 256;
        const int height = 4;

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.name = "Metallic Shimmer Sprite";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int x = 0; x < width; x++)
        {
            float normalizedX = x / (float)(width - 1);
            float distanceFromCenter = Mathf.Abs(normalizedX - 0.5f) * 2f;
            float broadGlow = Mathf.Pow(Mathf.Clamp01(1f - distanceFromCenter), 2f);
            float brightCore = Mathf.Pow(Mathf.Clamp01(1f - distanceFromCenter), 10f);
            Color shimmerColor = Color.Lerp(
                new Color(0.58f, 0.66f, 0.74f, 0f),
                new Color(0.96f, 0.98f, 1f, 0f),
                brightCore);
            shimmerColor.a = broadGlow * 0.22f + brightCore * 0.56f;

            for (int y = 0; y < height; y++)
            {
                texture.SetPixel(x, y, shimmerColor);
            }
        }

        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, width, height),
            new Vector2(0.5f, 0.5f),
            100f);
    }

    private Material CreateUiAlphaClipMaterial()
    {
        if (feedbackShimmerMaskMaterial != null)
        {
            return feedbackShimmerMaskMaterial;
        }

        Shader uiShader = Shader.Find("UI/Default");
        if (uiShader == null)
        {
            return null;
        }

        feedbackShimmerMaskMaterial = new Material(uiShader);
        feedbackShimmerMaskMaterial.name = "Feedback Shimmer Alpha Clip Mask";
        feedbackShimmerMaskMaterial.SetFloat("_UseUIAlphaClip", 1f);
        feedbackShimmerMaskMaterial.EnableKeyword("UNITY_UI_ALPHACLIP");
        return feedbackShimmerMaskMaterial;
    }

    private float GetRoundedRectAlpha(int x, int y, int size, int radius)
    {
        float px = x + 0.5f;
        float py = y + 0.5f;
        float minX = radius;
        float maxX = size - radius;
        float minY = radius;
        float maxY = size - radius;
        float cx = Mathf.Clamp(px, minX, maxX);
        float cy = Mathf.Clamp(py, minY, maxY);
        float distance = Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy));

        return Mathf.Clamp01(radius + 0.5f - distance);
    }

    private sealed class QuestionData
    {
        public string Text;
        public string[] Answers;
        public int CorrectAnswerIndex;
    }
}
