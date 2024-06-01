using CharacterAi.Models.DTO;

namespace CharacterAi.Models.HTTP
{
    internal class SearchResponse
    {
        public Result result { get; set; }
    }

    internal class Result
    {
        public Data data { get; set; }
    }

    internal class Data
    {
        public List<SearchResult> json { get; set; }
    }
}
