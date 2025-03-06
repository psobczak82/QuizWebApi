using QuizWebApi.Data;
using QuizWebApi.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace QuizWebApi.Services
{
    public class QuestionLoader
    {
        private readonly QuizDbContext _db;

        public QuestionLoader(QuizDbContext db)
        {
            _db = db;
        }

        public void LoadQuestionsFromFile(string filePath)
        {
            var lines = File.ReadAllLines(filePath);
            List<string> currentBlock = new();
            var blocks = new List<List<string>>();

            // Przykładowy separator: "===END===" kończy pytanie
            foreach (var line in lines)
            {
                if (line.StartsWith("===END==="))
                {
                    blocks.Add(new List<string>(currentBlock));
                    currentBlock.Clear();
                }
                else
                {
                    currentBlock.Add(line);
                }
            }

            // Parsowanie kazdego bloku
            foreach (var block in blocks)
            {
                ParseBlock(block);
            }
        }

        private void ParseBlock(List<string> block)
        {
            // Przykładowe wyodrębnianie linii: 
            // "QTYPE|SINGLE" -> questionType
            // "===ANSWERS===" -> początek listy odpowiedzi
            // "*Odp" -> IsCorrect = true
            // Cała reszta -> questionText itp.

            string qtype = "SINGLE";
            var qtypeLine = block.FirstOrDefault(l => l.StartsWith("QTYPE|"));
            if (qtypeLine != null)
            {
                var parts = qtypeLine.Split('|');
                if (parts.Length > 1)
                    qtype = parts[1].Trim();
            }

            int idxAnswers = block.FindIndex(l => l.StartsWith("===ANSWERS==="));
            if (idxAnswers < 0) return;

            // pytanie to wszystko do "===ANSWERS===" z pominięciem linii QTYPE
            var questionLines = block.Take(idxAnswers)
                .Where(l => !l.StartsWith("QTYPE|"))
                .ToList();
            string questionText = string.Join("\n", questionLines);

            var question = new Question
            {
                QuestionText = questionText,
                QuestionType = qtype
            };
            _db.Questions.Add(question);
            _db.SaveChanges();

            // Odpowiedzi to wiersze od idxAnswers + 1 do "===END==="
            var answersLines = block.Skip(idxAnswers + 1).ToList();
            foreach (var ansLine in answersLines)
            {
                if (string.IsNullOrWhiteSpace(ansLine)) continue;
                if (ansLine.StartsWith("===END===")) break;

                bool isCorrect = ansLine.TrimStart().StartsWith("*");
                var text = ansLine.TrimStart('*').Trim();

                var answer = new Answer
                {
                    QuestionId = question.QuestionId,
                    AnswerText = text,
                    IsCorrect = isCorrect
                };
                _db.Answers.Add(answer);
            }

            _db.SaveChanges();
        }
    }
}
