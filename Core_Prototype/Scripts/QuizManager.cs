using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class QuizManager : MonoBehaviour
{
    [System.Serializable]
    public class QuizQuestion
    {
        public string question;
        public string[] answers = new string[4];
        public int correctIndex;
    }

    public List<QuizQuestion> questions = new List<QuizQuestion>();

    [Header("UI References")]
    public GameObject panelQuestion;
    public GameObject panelScore;
    public GameObject panelScoreContent;
    public TextMeshProUGUI questionText;
    public Button[] answerButtons;
    public TextMeshProUGUI resultMessageText;
    public TextMeshProUGUI scoreText;
    public Button continueButton;
    public Button restartButton;

    [Header("Fragment Reveal")]
    public GameObject panelFragmentReveal;
    public Image fragmentIcon;
    public TextMeshProUGUI fragmentUnlockedText;
    public Button goToBaseSceneButton;

    [Header("Fragment Data")]
    public Item fragmentItem;  // assign the fragment ScriptableObject in Inspector

    private int currentQuestion = 0;
    private int score = 0;

    private void Start()
    {
        panelScore.SetActive(false);
        panelQuestion.SetActive(true);
        panelFragmentReveal.SetActive(false);

        ShowQuestion(currentQuestion);

        restartButton.onClick.AddListener(RestartQuiz);
        continueButton.onClick.AddListener(RevealFragment);
        goToBaseSceneButton.onClick.AddListener(() => SceneManager.LoadScene("2_BaseScene"));
    }

    private void ShowQuestion(int index)
    {
        var q = questions[index];
        questionText.text = q.question;

        for (int i = 0; i < 4; i++)
        {
            answerButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = q.answers[i];
            int closureIndex = i;
            answerButtons[i].onClick.RemoveAllListeners();
            answerButtons[i].onClick.AddListener(() => OnAnswerSelected(closureIndex));
        }
    }

    public void OnAnswerSelected(int index)
    {
        var q = questions[currentQuestion];
        if (index == q.correctIndex)
            score += 2;

        currentQuestion++;
        if (currentQuestion < questions.Count)
            ShowQuestion(currentQuestion);
        else
            EndQuiz();
    }

    private void EndQuiz()
    {
        panelQuestion.SetActive(false);
        panelScore.SetActive(true);

        scoreText.text = $"Your score is: {score}/{questions.Count * 2}";
        continueButton.gameObject.SetActive(false);
        restartButton.gameObject.SetActive(false);

        if (score >= 6)
        {
            resultMessageText.text = "You did well!";
            continueButton.gameObject.SetActive(true);
        }
        else
        {
            resultMessageText.text = "You can do better! Try again!";
            restartButton.gameObject.SetActive(true);
        }
    }

    public void RevealFragment()
    {
        Debug.Log("RevealFragment triggered");

        panelScoreContent.SetActive(false);
        panelFragmentReveal.SetActive(true);

        if (fragmentItem != null)
        {
            fragmentIcon.sprite = fragmentItem.icon;
            fragmentIcon.color = Color.white;
            fragmentUnlockedText.text = $"You unlocked: {fragmentItem.displayName}!";

            // Ensure it’s stored in inventory but NOT hotbar
            fragmentItem.isHotbarEligible = false;

            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.AddItem(fragmentItem);
                Debug.Log("Fragment added to Inventory (not hotbar).");
            }
        }
        else
        {
            fragmentUnlockedText.text = "No fragment assigned!";
            Debug.LogWarning("FragmentItem ScriptableObject not assigned in Inspector.");
        }
    }
    public void RestartQuiz()
    {
        currentQuestion = 0;
        score = 0;
        panelScore.SetActive(false);
        panelFragmentReveal.SetActive(false);
        panelQuestion.SetActive(true);
        panelScoreContent.SetActive(true);
        ShowQuestion(currentQuestion);
    }
}
