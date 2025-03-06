using System.Collections.Generic;

namespace QuizWebApi.Models
{
    public class Question
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; }
        public string QuestionType { get; set; }
        public string Explanation { get; set; }
        public string ECOInfo { get; set; }
        public ICollection<Answer> Answers { get; set; }
    }
}
