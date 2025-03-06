using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizWebApi.Data;
using QuizWebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuestionsController : ControllerBase
    {
        private readonly QuizDbContext _db;
        private readonly Random _rnd = new Random();
        // Tablica etykiet dla odpowiedzi – pierwsza odpowiedź to A, druga B itd.
        private readonly string[] answerLabels = new[] { "A", "B", "C", "D", "E" };

        public QuestionsController(QuizDbContext db)
        {
            _db = db;
        }

        // GET: api/questions?random=true
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Question>>> GetQuestions([FromQuery] bool random = false)
        {
            var questions = await _db.Questions
                                     .Include(q => q.Answers)
                                     .ToListAsync();

            if (random)
            {
                questions = questions.OrderBy(x => _rnd.Next()).ToList();
            }

            return questions;
        }

        // DTO dla pojedynczego sprawdzenia odpowiedzi
        public class CheckAnswersRequest
        {
            public int QuestionId { get; set; }
            public List<int> SelectedAnswers { get; set; }
        }

        public class CheckAnswersResult
        {
            public bool IsCorrect { get; set; }
            public List<int> CorrectAnswerIds { get; set; }
        }

        // DTO dla zbiorczego sprawdzenia odpowiedzi
        public class CheckAllRequest
        {
            public List<CheckAnswersRequest> Responses { get; set; }
        }

        public class QuestionReport
        {
            public int QuestionId { get; set; }
            public string QuestionText { get; set; }      // Nowe pole: treść pytania
            public bool IsCorrect { get; set; }
            public List<int> CorrectAnswerIds { get; set; }
            public List<int> UserSelectedAnswers { get; set; }
            public string CorrectAnswerLetters { get; set; }
            public string Explanation { get; set; }         // Wyjaśnienie pytania
        }

        public class CheckAllResult
        {
            public int TotalQuestions { get; set; }
            public int CorrectCount { get; set; }
            public int IncorrectCount { get; set; }
            public List<QuestionReport> Reports { get; set; }
        }

        // POST: api/questions/checkAll
        [HttpPost("checkAll")]
        public async Task<ActionResult<CheckAllResult>> CheckAllAnswers([FromBody] CheckAllRequest request)
        {
            var reports = new List<QuestionReport>();
            int correctCount = 0;
            foreach (var response in request.Responses)
            {
                var question = await _db.Questions
                    .Include(q => q.Answers)
                    .FirstOrDefaultAsync(q => q.QuestionId == response.QuestionId);
                if (question == null)
                    continue;
                var selected = (response.SelectedAnswers ?? new List<int>()).ToHashSet();
                var correctIds = question.Answers
                    .Where(a => a.IsCorrect)
                    .Select(a => a.AnswerId)
                    .ToHashSet();
                bool isCorrect = correctIds.All(id => selected.Contains(id)) && selected.All(id => correctIds.Contains(id));
                if (isCorrect)
                    correctCount++;

                // Mapowanie liter według kolejności - sortujemy odpowiedzi (np. po AnswerId)
                var sortedAnswers = question.Answers.OrderBy(a => a.AnswerId).ToList();
                Dictionary<int, string> letterMapping = new Dictionary<int, string>();
                for (int i = 0; i < sortedAnswers.Count; i++)
                {
                    string letter = i < answerLabels.Length ? answerLabels[i] : "?";
                    letterMapping[sortedAnswers[i].AnswerId] = letter;
                }
                // Pobieramy litery poprawnych odpowiedzi
                var correctLetters = sortedAnswers
                    .Where(a => a.IsCorrect)
                    .Select(a => letterMapping[a.AnswerId])
                    .ToList();
                string correctLettersString = string.Join(", ", correctLetters);

                reports.Add(new QuestionReport
                {
                    QuestionId = question.QuestionId,
                    QuestionText = question.QuestionText,     // Dodano treść pytania
                    IsCorrect = isCorrect,
                    CorrectAnswerIds = correctIds.ToList(),
                    UserSelectedAnswers = selected.ToList(),
                    CorrectAnswerLetters = correctLettersString,
                    Explanation = question.Explanation         // Dodano Explanation
                });
            }
            int total = request.Responses.Count;
            var result = new CheckAllResult
            {
                TotalQuestions = total,
                CorrectCount = correctCount,
                IncorrectCount = total - correctCount,
                Reports = reports
            };
            return result;
        }

        // POST: api/questions/check
        [HttpPost("check")]
        public async Task<ActionResult<CheckAnswersResult>> CheckAnswers([FromBody] CheckAnswersRequest request)
        {
            var question = await _db.Questions
                .Include(q => q.Answers)
                .FirstOrDefaultAsync(q => q.QuestionId == request.QuestionId);

            if (question == null)
                return NotFound();

            var selected = request.SelectedAnswers?.ToHashSet() ?? new HashSet<int>();
            var correctIds = question.Answers
                .Where(a => a.IsCorrect)
                .Select(a => a.AnswerId)
                .ToHashSet();

            bool allCorrectChosen = correctIds.All(id => selected.Contains(id));
            bool noWrongChosen = selected.All(id => correctIds.Contains(id));
            bool isCorrect = allCorrectChosen && noWrongChosen;

            return new CheckAnswersResult
            {
                IsCorrect = isCorrect,
                CorrectAnswerIds = correctIds.ToList()
            };
        }
    }
}
