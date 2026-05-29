using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public sealed class MillionaireQuizGame : MonoBehaviour
{
    private const int QuestionsCount = 8;
    private const int AnswersCount = 4;

    private static readonly Color BackgroundColor = new Color32(231, 232, 232, 255);
    private static readonly Color PanelColor = new Color32(23, 111, 193, 255);
    private static readonly Color ButtonColor = new Color32(23, 111, 193, 255);
    private static readonly Color ButtonHoverColor = new Color32(23, 111, 193, 255);
    private static readonly Color CorrectColor = new Color32(0, 176, 80, 255);
    private static readonly Color WrongColor = new Color32(255, 0, 0, 255);
    private static readonly Color AccentColor = new Color32(23, 111, 193, 255);
    private static readonly Color TextColor = new Color32(0, 0, 0, 255);
    private static readonly Color MutedTextColor = new Color32(51, 51, 51, 255);
    private static readonly Color QrLightColor = new Color32(255, 255, 255, 255);
    private static readonly Color QrDarkColor = new Color32(0, 0, 0, 255);

    private readonly QuestionData[] questions = new QuestionData[QuestionsCount];
    private QuestionData superQuestion;
    private Button[] answerButtons;
    private Text[] answerLabels;
    private Text questionLabel;
    private Text progressLabel;
    private Text feedbackTitleLabel;
    private Text feedbackMessageLabel;
    private Text finalScoreLabel;
    private Text finalRewardLabel;
    private GameObject startScreen;
    private GameObject questionScreen;
    private GameObject feedbackScreen;
    private GameObject finalScreen;
    private GameObject homeConfirmationOverlay;

    private Font regularFont;
    private Font boldFont;
    private Sprite logoSprite;
    private Sprite pmufLogoSprite;
    private Sprite roundedRectSprite;
    private int currentQuestionIndex;
    private int correctAnswers;
    private bool answerLocked;
    private bool superGameActive;
    private bool superGamePlayed;
    private bool feedbackWasInterruptedByHomeConfirmation;
    private Coroutine feedbackRoutine;

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
        ShowStartScreen();
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
            Answers = new[] { "(А) Постоянно действующее арбитражное учреждение", "(Б) Рекомендованный список арбитров", "(В) Единоличный арбитр или коллегия арбитров", "(Г) Арбитражный суд" }
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
            Answers = new[] { "(А) Отказать во взыскании", "(Б) Взыскать неустойку в размере ключевой ставки ЦБ РФ", "(В) Взыскать неустойку в двойном размере ключевой ставки ЦБ РФ", "(Г) Третейский суд никому ничего не должен. Он ведь третейский суд." }
        };

        questions[7] = new QuestionData
        {
            Text = "Право стороны на односторонний отказ от исполнения обязательства может быть реализовано:",
            CorrectAnswerIndex = 2,
            Answers = new[] { "(А) Если это право предусмотрено договором", "(Б) Если это право предусмотрено законом или договором", "(В) Разумно и добросовестно, если это право предусмотрено законом или договором", "(Г) Обязательство должно исполняться в любом случае" }
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

        startScreen = CreateScreen(canvasObject.transform, "Start Screen");
        CreateStartScreen(startScreen.transform);

        questionScreen = CreateScreen(canvasObject.transform, "Question Screen");
        CreateQuestionScreen(questionScreen.transform);

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

        CreateText(parent, "Title", "Кто хочет стать миллионером?", 72, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.58f), new Vector2(1100f, 110f), AccentColor);

        CreateText(parent, "Invite", "Нажмите кнопку, чтобы начать участие в викторине", 34, FontStyle.Normal,
            TextAnchor.MiddleCenter, new Vector2(0.5f, 0.45f), new Vector2(1000f, 80f), AccentColor);

        CreateButton(parent, "Start Button", "Начать", new Vector2(0.5f, 0.3f), new Vector2(480f, 118f), StartQuiz);
    }

    private void CreateLogo(Transform parent)
    {
        CreateLogoImage(parent, pmufLogoSprite, "PMUF Logo", new Vector2(0.32f, 0.78f), new Vector2(360f, 110f));
        CreateLogoImage(parent, logoSprite, "Brand Logo", new Vector2(0.62f, 0.78f), new Vector2(600f, 250f));
    }

    private void CreateLogoImage(Transform parent, Sprite sprite, string name, Vector2 anchor, Vector2 size)
    {
        if (sprite == null)
        {
            return;
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
    }

    private void CreateQuestionScreen(Transform parent)
    {
        CreateHomeButton(parent);

        progressLabel = CreateText(parent, "Progress", string.Empty, 30, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.88f), new Vector2(460f, 58f), AccentColor);

        RectTransform questionPanel = CreatePanel(parent, "Question Panel", new Vector2(0.5f, 0.68f), new Vector2(1460f, 300f), PanelColor);
        questionLabel = CreateText(questionPanel, "Question", string.Empty, 38, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(1360f, 240f), Color.white);
        questionLabel.resizeTextForBestFit = true;
        questionLabel.resizeTextMinSize = 18;
        questionLabel.resizeTextMaxSize = 38;

        answerButtons = new Button[AnswersCount];
        answerLabels = new Text[AnswersCount];

        Vector2[] positions =
        {
            new Vector2(0.29f, 0.40f),
            new Vector2(0.71f, 0.40f),
            new Vector2(0.29f, 0.22f),
            new Vector2(0.71f, 0.22f)
        };

        for (int i = 0; i < AnswersCount; i++)
        {
            int answerIndex = i;
            Button button = CreateButton(parent, "Answer " + (i + 1), string.Empty, positions[i], new Vector2(760f, 118f),
                () => SelectAnswer(answerIndex));
            Text label = button.GetComponentInChildren<Text>();
            label.alignment = TextAnchor.MiddleCenter;
            label.fontSize = 26;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 18;
            label.resizeTextMaxSize = 26;
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

        feedbackTitleLabel = CreateText(panel, "Feedback Title", string.Empty, 64, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.65f), new Vector2(900f, 100f), Color.white);

        feedbackMessageLabel = CreateText(panel, "Feedback Message", string.Empty, 34, FontStyle.Normal, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.43f), new Vector2(920f, 110f), Color.white);
    }

    private void CreateFinalScreen(Transform parent)
    {
        CreateHomeButton(parent);

        CreateText(parent, "Final Title", "Викторина завершена", 56, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.9f), new Vector2(1000f, 80f), AccentColor);

        CreateQrPlaceholder(parent);

        finalScoreLabel = CreateText(parent, "Final Score", string.Empty, 34, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.37f), new Vector2(1200f, 70f), TextColor);

        finalRewardLabel = CreateText(parent, "Final Reward Message", string.Empty, 28, FontStyle.Normal, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.23f), new Vector2(1420f, 140f), TextColor);
        finalRewardLabel.resizeTextForBestFit = true;
        finalRewardLabel.resizeTextMinSize = 20;
        finalRewardLabel.resizeTextMaxSize = 28;

        CreateButton(parent, "Restart Button", "Начать заново", new Vector2(0.5f, 0.12f), new Vector2(430f, 86f), StartQuiz);
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

        RectTransform panel = CreatePanel(parent, "Home Confirmation Panel", new Vector2(0.5f, 0.5f), new Vector2(760f, 360f), BackgroundColor);

        Text title = CreateText(panel, "Home Confirmation Title", "Вернуться на главный экран?", 38, FontStyle.Bold,
            TextAnchor.MiddleCenter, new Vector2(0.5f, 0.66f), new Vector2(620f, 90f), AccentColor);
        title.resizeTextForBestFit = true;
        title.resizeTextMinSize = 26;
        title.resizeTextMaxSize = 38;

        CreateText(panel, "Home Confirmation Text", "Текущая игра будет прервана.", 28, FontStyle.Normal,
            TextAnchor.MiddleCenter, new Vector2(0.5f, 0.48f), new Vector2(620f, 60f), TextColor);

        CreateButton(panel, "Confirm Home Button", "Да", new Vector2(0.32f, 0.22f), new Vector2(220f, 78f), ConfirmReturnHome);
        CreateButton(panel, "Cancel Home Button", "Нет", new Vector2(0.68f, 0.22f), new Vector2(220f, 78f), HideHomeConfirmation);
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
        currentQuestionIndex = 0;
        correctAnswers = 0;
        superGameActive = false;
        superGamePlayed = false;
        ShowQuestion();
    }

    private void ShowQuestion()
    {
        answerLocked = false;
        SetActiveScreen(questionScreen);

        QuestionData question = GetCurrentQuestion();
        progressLabel.text = superGameActive ? "Суперигра" : "Вопрос " + (currentQuestionIndex + 1) + " из " + questions.Length;
        progressLabel.fontSize = superGameActive ? 52 : 30;
        questionLabel.text = question.Text;

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

        if (superGameActive)
        {
            superGamePlayed = true;
        }

        if (!superGameActive && isCorrect)
        {
            correctAnswers++;
        }

        for (int i = 0; i < answerButtons.Length; i++)
        {
            answerButtons[i].interactable = false;

            if (i == answerIndex)
            {
                SetButtonColor(answerButtons[i], isCorrect ? CorrectColor : WrongColor);
            }
            else if (i == question.CorrectAnswerIndex)
            {
                SetButtonColor(answerButtons[i], CorrectColor);
            }
        }

        feedbackTitleLabel.text = isCorrect ? "Правильно!" : "Неправильно";
        feedbackTitleLabel.color = isCorrect ? CorrectColor : WrongColor;
        feedbackMessageLabel.text = superGameActive
            ? "Суперигра завершена. Переходим к призовому QR-коду."
            : isCorrect
                ? "Ответ засчитан. Переходим к следующему вопросу."
                : "Выбран неверный вариант. Правильный ответ подсвечен зеленым.";

        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
        }

        feedbackRoutine = StartCoroutine(ShowFeedbackThenContinue());
    }

    private IEnumerator ShowFeedbackThenContinue()
    {
        yield return new WaitForSeconds(0.8f);
        SetActiveScreen(feedbackScreen);
        yield return new WaitForSeconds(1.6f);

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

        if (correctAnswers >= 4)
        {
            ShowSuperGame();
            yield break;
        }

        ShowFinalScreen();
    }

    private void ShowStartScreen()
    {
        HideHomeConfirmation();
        SetActiveScreen(startScreen);
    }

    private void ShowHomeConfirmation()
    {
        feedbackWasInterruptedByHomeConfirmation = feedbackScreen.activeSelf && feedbackRoutine != null;

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
        SetActiveScreen(startScreen);
    }

    private void ShowFinalScreen()
    {
        superGameActive = false;
        finalScoreLabel.text = "Ваш результат в основной игре: " + correctAnswers + " из " + questions.Length;
        finalScoreLabel.color = AccentColor;
        finalRewardLabel.text = superGamePlayed
            ? "Поздравляем!!! В качестве приза получите ЛЮБОЙ напиток в нашем баре"
            : "Попробуйте сыграть еще раз. В качестве поощрения за участие Вам предлагается приз в виде БЕЗАЛКОГОЛЬНОГО напитка в нашем баре. Сфотографируйте QR-код и предъявите бармену";
        finalRewardLabel.color = AccentColor;
        SetActiveScreen(finalScreen);
    }

    private void ShowSuperGame()
    {
        superGameActive = true;
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
        return luminance > 0.55f ? TextColor : Color.white;
    }

    private void SetActiveScreen(GameObject activeScreen)
    {
        startScreen.SetActive(activeScreen == startScreen);
        questionScreen.SetActive(activeScreen == questionScreen);
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

        Texture2D logoTexture = Resources.Load<Texture2D>("Brand/logo_eps_rus");
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

        roundedRectSprite = CreateRoundedRectSprite();
    }

    private Font GetFontForStyle(FontStyle style)
    {
        return style == FontStyle.Bold ? boldFont : regularFont;
    }

    private FontStyle GetRuntimeFontStyle(FontStyle style)
    {
        return style == FontStyle.Bold && boldFont == regularFont ? FontStyle.Bold : FontStyle.Normal;
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
