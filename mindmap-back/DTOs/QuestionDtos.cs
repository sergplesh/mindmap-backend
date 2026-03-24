using System.ComponentModel.DataAnnotations;

namespace KnowledgeMap.Backend.DTOs
{
    public class CreateQuestionDto
    {
        [Required]
        public int NodeId { get; set; }

        [Required]
        public string QuestionText { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^(single_choice|multiple_choice)$")]
        public string QuestionType { get; set; } = "single_choice";

        public List<AnswerOptionDto>? AnswerOptions { get; set; }
    }

    public class UpdateQuestionDto
    {
        [Required]
        public string QuestionText { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^(single_choice|multiple_choice)$")]
        public string QuestionType { get; set; } = "single_choice";
    }

    public class VerifyAnswersDto
    {
        [Required]
        public int NodeId { get; set; }
        public List<UserAnswerDto> Answers { get; set; } = new List<UserAnswerDto>();
    }

    public class UserAnswerDto
    {
        public int QuestionId { get; set; }
        public int? SelectedOptionId { get; set; }
        public List<int>? SelectedOptionIds { get; set; }
    }
}
