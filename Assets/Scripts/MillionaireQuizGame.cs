using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public sealed class MillionaireQuizGame : MonoBehaviour
{
    private const int QuestionsCount = 5;
    private const int AnswersCount = 4;

    private static readonly Color BackgroundColor = new Color(0.03f, 0.05f, 0.16f);
    private static readonly Color PanelColor = new Color(0.08f, 0.12f, 0.31f);
    private static readonly Color ButtonColor = new Color(0.13f, 0.2f, 0.46f);
    private static readonly Color ButtonHoverColor = new Color(0.18f, 0.29f, 0.64f);
    private static readonly Color CorrectColor = new Color(0.13f, 0.58f, 0.27f);
    private static readonly Color WrongColor = new Color(0.75f, 0.16f, 0.18f);
    private static readonly Color GoldColor = new Color(0.96f, 0.73f, 0.24f);

    private readonly QuestionData[] questions = new QuestionData[QuestionsCount];
    private Button[] answerButtons;
    private Text[] answerLabels;
    private Text questionLabel;
    private Text progressLabel;
    private Text feedbackTitleLabel;
    private Text feedbackMessageLabel;
    private Text finalScoreLabel;
    private GameObject startScreen;
    private GameObject questionScreen;
    private GameObject feedbackScreen;
    private GameObject finalScreen;

    private Font uiFont;
    private int currentQuestionIndex;
    private int correctAnswers;
    private bool answerLocked;
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
        uiFont = GetBuiltinFont();
        CreateQuestions();
        CreateEventSystem();
        CreateInterface();
        ShowStartScreen();
    }

    private void CreateQuestions()
    {
        questions[0] = new QuestionData
        {
            Text = "кто?",
            CorrectAnswerIndex = 0,
            Answers = new[] { "собака", "кошка", "хомяк", "кролик" }
        };

        questions[1] = new QuestionData
        {
            Text = "где?",
            CorrectAnswerIndex = 1,
            Answers = new[] { "в городе", "в деревне", "в поле", "в космосе" }
        };

        questions[2] = new QuestionData
        {
            Text = "когда?",
            CorrectAnswerIndex = 2,
            Answers = new[] { "сегодня", "завтра", "вчера", "никогда" }
        };

        questions[3] = new QuestionData
        {
            Text = "что?",
            CorrectAnswerIndex = 3,
            Answers = new[] { "позавтракал", "поспал", "погулял", "поработал" }
        };

        questions[4] = new QuestionData
        {
            Text = "почему?",
            CorrectAnswerIndex = 0,
            Answers = new[] { "захотелось", "надо было", "приказали", "попросили" }
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
    }

    private void CreateStartScreen(Transform parent)
    {
        CreateText(parent, "Title", "Кто хочет стать миллионером?", 72, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.64f), new Vector2(1100f, 110f), GoldColor);

        CreateText(parent, "Invite", "Нажмите кнопку, чтобы начать участие в викторине", 34, FontStyle.Normal,
            TextAnchor.MiddleCenter, new Vector2(0.5f, 0.51f), new Vector2(1000f, 80f), Color.white);

        CreateButton(parent, "Start Button", "Начать", new Vector2(0.5f, 0.36f), new Vector2(360f, 94f), StartQuiz);
    }

    private void CreateQuestionScreen(Transform parent)
    {
        progressLabel = CreateText(parent, "Progress", string.Empty, 30, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.88f), new Vector2(460f, 58f), GoldColor);

        RectTransform questionPanel = CreatePanel(parent, "Question Panel", new Vector2(0.5f, 0.66f), new Vector2(1320f, 210f), PanelColor);
        questionLabel = CreateText(questionPanel, "Question", string.Empty, 44, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(1180f, 150f), Color.white);

        answerButtons = new Button[AnswersCount];
        answerLabels = new Text[AnswersCount];

        Vector2[] positions =
        {
            new Vector2(0.32f, 0.41f),
            new Vector2(0.68f, 0.41f),
            new Vector2(0.32f, 0.25f),
            new Vector2(0.68f, 0.25f)
        };

        for (int i = 0; i < AnswersCount; i++)
        {
            int answerIndex = i;
            Button button = CreateButton(parent, "Answer " + (i + 1), string.Empty, positions[i], new Vector2(610f, 100f),
                () => SelectAnswer(answerIndex));
            Text label = button.GetComponentInChildren<Text>();
            label.alignment = TextAnchor.MiddleCenter;
            label.fontSize = 30;
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
        RectTransform panel = CreatePanel(parent, "Feedback Panel", new Vector2(0.5f, 0.53f), new Vector2(1120f, 470f), PanelColor);

        feedbackTitleLabel = CreateText(panel, "Feedback Title", string.Empty, 64, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.65f), new Vector2(900f, 100f), Color.white);

        feedbackMessageLabel = CreateText(panel, "Feedback Message", string.Empty, 34, FontStyle.Normal, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.43f), new Vector2(920f, 110f), Color.white);
    }

    private void CreateFinalScreen(Transform parent)
    {
        CreateText(parent, "Final Title", "Викторина завершена", 70, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.64f), new Vector2(1000f, 100f), GoldColor);

        finalScoreLabel = CreateText(parent, "Final Score", string.Empty, 40, FontStyle.Normal, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(980f, 90f), Color.white);

        CreateButton(parent, "Restart Button", "Начать заново", new Vector2(0.5f, 0.34f), new Vector2(430f, 94f), StartQuiz);
    }

    private void StartQuiz()
    {
        currentQuestionIndex = 0;
        correctAnswers = 0;
        ShowQuestion();
    }

    private void ShowQuestion()
    {
        answerLocked = false;
        SetActiveScreen(questionScreen);

        QuestionData question = questions[currentQuestionIndex];
        progressLabel.text = "Вопрос " + (currentQuestionIndex + 1) + " из " + questions.Length;
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
        QuestionData question = questions[currentQuestionIndex];
        bool isCorrect = answerIndex == question.CorrectAnswerIndex;

        if (isCorrect)
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
        feedbackMessageLabel.text = isCorrect
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

        currentQuestionIndex++;

        if (currentQuestionIndex < questions.Length)
        {
            ShowQuestion();
            yield break;
        }

        ShowFinalScreen();
    }

    private void ShowStartScreen()
    {
        SetActiveScreen(startScreen);
    }

    private void ShowFinalScreen()
    {
        finalScoreLabel.text = "Ваш результат: " + correctAnswers + " из " + questions.Length;
        SetActiveScreen(finalScreen);
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

    private RectTransform CreatePanel(Transform parent, string name, Vector2 anchor, Vector2 size, Color color)
    {
        GameObject panelObject = new GameObject(name);
        panelObject.transform.SetParent(parent, false);

        RectTransform rectTransform = panelObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = size;

        Image image = panelObject.AddComponent<Image>();
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
        image.color = ButtonColor;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);
        button.colors = CreateButtonColors(ButtonColor);

        Text text = CreateText(rectTransform, "Label", label, 36, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), size, Color.white);
        text.raycastTarget = false;

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
        text.font = uiFont;
        text.fontSize = fontSize;
        text.fontStyle = style;
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
        colors.pressedColor = GoldColor;
        colors.selectedColor = ButtonHoverColor;
        colors.disabledColor = normalColor;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        return colors;
    }

    private void SetButtonColor(Button button, Color color)
    {
        Image image = button.GetComponent<Image>();
        image.color = color;
        button.colors = CreateButtonColors(color);
    }

    private void SetActiveScreen(GameObject activeScreen)
    {
        startScreen.SetActive(activeScreen == startScreen);
        questionScreen.SetActive(activeScreen == questionScreen);
        feedbackScreen.SetActive(activeScreen == feedbackScreen);
        finalScreen.SetActive(activeScreen == finalScreen);
    }

    private Font GetBuiltinFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private sealed class QuestionData
    {
        public string Text;
        public string[] Answers;
        public int CorrectAnswerIndex;
    }
}
