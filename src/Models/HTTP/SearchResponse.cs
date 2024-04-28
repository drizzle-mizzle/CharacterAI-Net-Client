using CharacterAiNet.Models.DTO;

namespace CharacterAiNet.Models.HTTP
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
