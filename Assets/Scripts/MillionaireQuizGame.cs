using System.Collections;
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
    private const string DebugStartQuestion = "";
    private const bool DebugForceSuperGameAccess = false;

    private static readonly Color BackgroundColor = new Color32(0, 91, 168, 255);
    private static readonly Color PanelColor = new Color32(254, 254, 254, 255);
    private static readonly Color ButtonColor = new Color32(254, 254, 254, 255);
    private static readonly Color ButtonHoverColor = new Color32(254, 254, 254, 255);
    private static readonly Color SelectedAnswerBlinkColor = new Color32(218, 228, 23, 255);
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
    private RectTransform correctFeedbackShimmer;
    private Text questionLabel;
    private Text progressLabel;
    private Text feedbackTitleLabel;
    private Text finalScoreLabel;
    private Text finalRewardLabel;
    private GameObject startScreen;
    private GameObject questionScreen;
    private GameObject superGameIntroScreen;
    private GameObject feedbackScreen;
    private GameObject finalScreen;
    private GameObject homeConfirmationOverlay;
    private Image superLogoImage;
    private Button startButton;

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
    private Sprite millionaireQuestionSprite;
    private Sprite millionaireAnswerSprite;
    private VideoPlayer backgroundVideoPlayer;
    private RenderTexture backgroundVideoTexture;
    private int currentQuestionIndex;
    private int correctAnswers;
    private int selectedAnswerIndex;
    private bool answerLocked;
    private bool superGameActive;
    private bool superGamePlayed;
    private bool selectedAnswerWasCorrect;
    private bool feedbackWasInterruptedByHomeConfirmation;
    private Coroutine feedbackRoutine;
    private Coroutine countdownRoutine;
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
        LoadBrandAssets();
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
            Text = "Что у юристов означает выражение: «Применить триста тридцать третью»?",
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
            Answers = new[] { "(А) Договорная", "(Б) Процессуальная", "(В) Смешанная", "(Г) Чтобы разобраться нужно оформить подписку на журнал «Третейский суд»." }
        };

        questions[6] = new QuestionData
        {
            Text = "В договоре поставки отсутствует соглашение сторон о неустойке, но истец требует ее взыскать. В этом случае третейский суд должен:",
            CorrectAnswerIndex = 1,
            Answers = new[] { "(А) Отказать во взыскании", "(Б) Взыскать неустойку в размере\nключевой ставки ЦБ РФ", "(В) Взыскать неустойку в двойном размере ключевой ставки ЦБ РФ", "(Г) Третейский суд никому ничего не\nдолжен. Он ведь третейский суд." }
        };

        questions[7] = new QuestionData
        {
            Text = "Право стороны на односторонний отказ от исполнения обязательства может быть реализовано:",
            CorrectAnswerIndex = 2,
            Answers = new[] { "(А) Если это право предусмотрено договором", "(Б) Если это право предусмотрено законом или договором", "(В) Разумно и добросовестно,\nесли это право предусмотрено\nзаконом или договором", "(Г) Обязательство должно исполняться\nв любом случае" }
        };

        superQuestion = new QuestionData
        {
            Text = "Этот ученый правовед в 1981 году защитил кандидатскую диссертацию на тему: «Динамика обязательственного правоотношения и гражданско-правовая ответственность», а в 1994 году защитил докторскую диссертацию на тему «Проблемы правового режима предпринимательства». Назовите этого ученого:",
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
        CreateQuestionScreen(questionScreen.transform);

        superGameIntroScreen = CreateScreen(canvasObject.transform, "Super Game Intro Screen");
        CreateSuperGameIntroScreen(superGameIntroScreen.transform);

        feedbackScreen = CreateScreen(canvasObject.transform, "Feedback Screen");
        CreateFeedbackScreen(feedbackScreen.transform);

        finalScreen = CreateScreen(canvasObject.transform, "Final Screen");
        CreateFinalScreen(finalScreen.transform);

        homeConfirmationOverlay = CreateScreen(canvasObject.transform, "Home Confirmation Overlay");
        CreateHomeConfirmationOverlay(homeConfirmationOverlay.transform);
        homeConfirmationOverlay.SetActive(false);
    }

    private void CreateStartScreen(Transform parent)
    {
        CreateLogo(parent);

        superLogoImage = CreateLogoImage(parent, superLogoSprite, "Super Logo", new Vector2(0.5f, 0.47f), new Vector2(1180f * 2f, 270f * 2f));

        startButton = CreateMillionaireAnswerButton(parent, "Start Button", new Vector2(0.5f, 0.12f), new Vector2(620f, 118f), StartCountdown);
        Text startButtonLabel = startButton.GetComponentInChildren<Text>();
        startButtonLabel.text = "Начать";
        startButtonLabel.fontSize = 70;
    }

    private void CreateSuperGameIntroScreen(Transform parent)
    {
        CreateLogoImage(parent, superGameLogoSprite, "Super Game Logo", new Vector2(0.5f, 0.47f), new Vector2(1180f * 4f, 270f * 4f));
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
        float logo_multiplier = 1.5f;
        CreateLogoImage(parent, pmufLogoSprite, "PMUF Logo", new Vector2(0.21f, 0.85f), new Vector2(360f * logo_multiplier, 150f * logo_multiplier));
        CreateLogoImage(parent, minuLogoSprite, "Brand Logo", new Vector2(0.52f, 0.85f), new Vector2(360f * logo_multiplier, 150f * logo_multiplier));
        CreateLogoImage(parent, logoSprite, "MINU Logo", new Vector2(0.79f, 0.85f), new Vector2(360f * logo_multiplier, 150f * logo_multiplier));
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
        CreateHomeButton(parent);

        progressLabel = CreateText(parent, "Progress", string.Empty, 50, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.82f), new Vector2(460f, 58f), AccentColor);

        questionPanel = CreateMillionairePanel(parent, "Question Panel", new Vector2(0.5f, 0.62f), new Vector2(1640f, 270f), millionaireQuestionSprite);
        questionLabel = CreateText(questionPanel, "Question", string.Empty, 60, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(1460f, 214f), FrameTextColor);
        questionLabel.resizeTextForBestFit = true;
        questionLabel.resizeTextMinSize = 14;
        questionLabel.resizeTextMaxSize = 60;

        answerButtons = new Button[AnswersCount];
        answerLabels = new Text[AnswersCount];

        Vector2[] positions =
        {
            new Vector2(0.27f, 0.365f),
            new Vector2(0.73f, 0.365f),
            new Vector2(0.27f, 0.145f),
            new Vector2(0.73f, 0.145f)
        };

        for (int i = 0; i < AnswersCount; i++)
        {
            int answerIndex = i;
            Button button = CreateMillionaireAnswerButton(parent, "Answer " + (i + 1), positions[i], new Vector2(800f, 180f),
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

    private void CreateFeedbackScreen(Transform parent)
    {
        CreateHomeButton(parent);

        RectTransform panel = CreatePanel(parent, "Feedback Panel", new Vector2(0.5f, 0.53f), new Vector2(1120f, 470f), PanelColor);

        GameObject shimmerMaskObject = new GameObject("Feedback Shimmer Mask");
        shimmerMaskObject.transform.SetParent(panel, false);
        RectTransform shimmerMaskRect = shimmerMaskObject.AddComponent<RectTransform>();
        shimmerMaskRect.anchorMin = Vector2.zero;
        shimmerMaskRect.anchorMax = Vector2.one;
        shimmerMaskRect.offsetMin = Vector2.zero;
        shimmerMaskRect.offsetMax = Vector2.zero;

        Image shimmerMaskImage = shimmerMaskObject.AddComponent<Image>();
        shimmerMaskImage.sprite = roundedRectSprite;
        shimmerMaskImage.type = Image.Type.Sliced;
        shimmerMaskImage.raycastTarget = false;

        Mask shimmerMask = shimmerMaskObject.AddComponent<Mask>();
        shimmerMask.showMaskGraphic = false;

        GameObject shimmerObject = new GameObject("Correct Answer Gold Shimmer");
        shimmerObject.transform.SetParent(shimmerMaskRect, false);
        correctFeedbackShimmer = shimmerObject.AddComponent<RectTransform>();
        correctFeedbackShimmer.anchorMin = new Vector2(0.5f, 0.5f);
        correctFeedbackShimmer.anchorMax = new Vector2(0.5f, 0.5f);
        correctFeedbackShimmer.pivot = new Vector2(0.5f, 0.5f);
        correctFeedbackShimmer.sizeDelta = new Vector2(360f, 650f);
        correctFeedbackShimmer.localEulerAngles = new Vector3(0f, 0f, -14f);

        Image shimmerImage = shimmerObject.AddComponent<Image>();
        shimmerImage.sprite = goldShimmerSprite;
        shimmerImage.type = Image.Type.Simple;
        shimmerImage.raycastTarget = false;
        shimmerObject.SetActive(false);

        feedbackTitleLabel = CreateText(panel, "Feedback Title", string.Empty, 120, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(1000f, 220f), FrameTextColor);
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

        finalScoreLabel = CreateText(parent, "Final Score", string.Empty, 42, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.39f), new Vector2(1400f, 86f), TextColor);

        finalRewardLabel = CreateText(parent, "Final Reward Message", string.Empty, 42, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.27f), new Vector2(1580f, 210f), TextColor);
        finalRewardLabel.resizeTextForBestFit = true;
        finalRewardLabel.resizeTextMinSize = 28;
        finalRewardLabel.resizeTextMaxSize = 42;

        Button restartButton = CreateMillionaireAnswerButton(parent, "Restart Button", new Vector2(0.5f, 0.12f), new Vector2(620f, 104f), StartQuiz);
        Text restartButtonLabel = restartButton.GetComponentInChildren<Text>();
        restartButtonLabel.text = "Начать заново";
        restartButtonLabel.fontSize = 60;
    }

    private void CreateHomeButton(Transform parent)
    {
        Button button = CreateButton(parent, "Home Button", "⌂", new Vector2(0.95f, 0.92f), new Vector2(86f, 72f), ShowHomeConfirmation);
        Text label = button.GetComponentInChildren<Text>();
        label.fontSize = 38;
        label.resizeTextForBestFit = false;
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

        Button confirmButton = CreateButton(panel, "Confirm Home Button", "Да", new Vector2(0.32f, 0.21f), new Vector2(280f, 104f), ConfirmReturnHome);
        confirmButton.GetComponentInChildren<Text>().fontSize = 50;

        Button cancelButton = CreateButton(panel, "Cancel Home Button", "Нет", new Vector2(0.68f, 0.21f), new Vector2(280f, 104f), HideHomeConfirmation);
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
        SetActiveScreen(questionScreen);

        QuestionData question = GetCurrentQuestion();
        progressLabel.text = superGameActive ? "Суперигра" : "Вопрос " + (currentQuestionIndex + 1) + " из " + questions.Length;
        progressLabel.fontSize = superGameActive ? 52 : 50;
        Vector2 progressAnchor = superGameActive ? new Vector2(0.5f, 0.88f) : new Vector2(0.5f, 0.82f);
        progressLabel.rectTransform.anchorMin = progressAnchor;
        progressLabel.rectTransform.anchorMax = progressAnchor;
        questionLabel.text = question.Text;
        questionLabel.fontSize = superGameActive ? 44 : 60;
        questionLabel.resizeTextMaxSize = superGameActive ? 44 : 60;
        Vector2 questionAnchor = superGameActive ? new Vector2(0.5f, 0.655f) : new Vector2(0.5f, 0.62f);
        questionPanel.anchorMin = questionAnchor;
        questionPanel.anchorMax = questionAnchor;
        questionPanel.sizeDelta = superGameActive ? new Vector2(1640f, 330f) : new Vector2(1640f, 270f);
        questionLabel.rectTransform.sizeDelta = superGameActive ? new Vector2(1460f, 274f) : new Vector2(1460f, 214f);

        for (int i = 0; i < answerButtons.Length; i++)
        {
            answerButtons[i].interactable = true;
            answerLabels[i].text = question.Answers[i];
            SetButtonColor(answerButtons[i], ButtonColor);
        }
    }

    private void SelectAnswer(int answerIndex)
    {
        if (answerLocked)
        {
            return;
        }

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

        for (int i = 0; i < blinkCount; i++)
        {
            SetButtonColor(answerButtons[selectedAnswerIndex], SelectedAnswerBlinkColor);
            yield return new WaitForSeconds(blinkInterval);
            SetButtonColor(answerButtons[selectedAnswerIndex], ButtonColor);
            yield return new WaitForSeconds(blinkInterval);
        }

        correctFeedbackShimmer.gameObject.SetActive(false);
        feedbackTitleLabel.rectTransform.anchoredPosition = Vector2.zero;
        feedbackTitleLabel.rectTransform.localEulerAngles = Vector3.zero;
        feedbackTitleLabel.color = selectedAnswerWasCorrect
            ? new Color(FrameTextColor.r, FrameTextColor.g, FrameTextColor.b, 0f)
            : FrameTextColor;
        SetActiveScreen(feedbackScreen);

        if (selectedAnswerWasCorrect)
        {
            StartCoroutine(PlayCorrectFeedbackAnimation());
        }
        else
        {
            StartCoroutine(PlayIncorrectFeedbackTitleDrop());
        }

        yield return new WaitForSeconds(5f);
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
            feedbackTitleLabel.color = new Color(
                FrameTextColor.r,
                FrameTextColor.g,
                FrameTextColor.b,
                smoothProgress);
            yield return null;
        }

        feedbackTitleLabel.color = FrameTextColor;
        yield return PlayCorrectFeedbackShimmer();
    }

    private IEnumerator PlayCorrectFeedbackShimmer()
    {
        const float duration = 3f;
        const float startX = -760f;
        const float endX = 760f;

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

        yield return AnimateFeedbackTitlePose(
            titleTransform,
            new Vector2(0f, 650f),
            0f,
            new Vector2(0f, 0f),
            0f,
            1.5f,
            true);

        yield return AnimateFeedbackTitlePose(
            titleTransform,
            Vector2.zero,
            0f,
            new Vector2(0f, -18f),
            18f,
            0.28f,
            false);

        yield return AnimateFeedbackTitlePose(
            titleTransform,
            new Vector2(0f, -18f),
            18f,
            new Vector2(0f, 10f),
            -11f,
            0.38f,
            false);

        yield return AnimateFeedbackTitlePose(
            titleTransform,
            new Vector2(0f, 10f),
            -11f,
            Vector2.zero,
            0f,
            0.5f,
            false);
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
        ShowStartScreen();
    }

    private void ShowFinalScreen()
    {
        superGameActive = false;
        finalScoreLabel.text = "Ваш результат в основной игре: " + correctAnswers + " из " + questions.Length;
        finalScoreLabel.color = AccentColor;
        //superGamePlayed = true;
        finalRewardLabel.text = superGamePlayed
            ? "Поздравляем!!! В качестве приза получите ЛЮБОЙ напиток в нашем баре. Сфотографируйте QR-код и предъявите бармену."
            : "Попробуйте сыграть еще раз. В качестве поощрения за участие Вам предлагается приз в виде БЕЗАЛКОГОЛЬНОГО напитка в нашем баре. Сфотографируйте QR-код и предъявите бармену.";
        finalRewardLabel.color = AccentColor;
        SetActiveScreen(finalScreen);
    }

    private void ShowSuperGame()
    {
        superGameActive = true;
        SetActiveScreen(superGameIntroScreen);
        superGameIntroRoutine = StartCoroutine(ShowSuperGameAfterIntro());
    }

    private IEnumerator ShowSuperGameAfterIntro()
    {
        yield return new WaitForSeconds(3f);
        superGameIntroRoutine = null;
        ShowQuestion();
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

        Texture2D minuLogoTexture = Resources.Load<Texture2D>("Brand/logo_MINU");
        if (minuLogoTexture != null)
        {
            minuLogoSprite = Sprite.Create(
                minuLogoTexture,
                new Rect(0f, 0f, minuLogoTexture.width, minuLogoTexture.height),
                new Vector2(0.5f, 0.5f),
                100f);
        }

        Texture2D superLogoTexture = Resources.Load<Texture2D>("Brand/logo_super_puper_new");
        if (superLogoTexture != null)
        {
            superLogoSprite = Sprite.Create(
                superLogoTexture,
                new Rect(0f, 0f, superLogoTexture.width, superLogoTexture.height),
                new Vector2(0.5f, 0.5f),
                100f);
        }

        superGameLogoSprite = LoadSpriteFromResources("Brand/logo_supergame");

        countdownLogoSprites = new[]
        {
            LoadSpriteFromResources("Brand/logo_number_3"),
            LoadSpriteFromResources("Brand/logo_number_2"),
            LoadSpriteFromResources("Brand/logo_number_1")
        };

        roundedRectSprite = CreateRoundedRectSprite();
        goldShimmerSprite = CreateGoldShimmerSprite();
        millionaireQuestionSprite = LoadSpriteFromResources("Brand/question_field");
        millionaireAnswerSprite = LoadSpriteFromResources("Brand/answer_field");
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
        texture.name = "Gold Shimmer Sprite";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int x = 0; x < width; x++)
        {
            float normalizedX = x / (float)(width - 1);
            float distanceFromCenter = Mathf.Abs(normalizedX - 0.5f) * 2f;
            float broadGlow = Mathf.Pow(Mathf.Clamp01(1f - distanceFromCenter), 2f);
            float brightCore = Mathf.Pow(Mathf.Clamp01(1f - distanceFromCenter), 10f);
            Color shimmerColor = Color.Lerp(
                new Color(1f, 0.64f, 0.08f, 0f),
                new Color(1f, 0.96f, 0.66f, 0f),
                brightCore);
            shimmerColor.a = broadGlow * 0.28f + brightCore * 0.48f;

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
