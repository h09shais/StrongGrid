namespace StrongGrid.Models.Search.Legacy
{
	/// <summary>
	/// Filter the result of a search for the value of a field to be like a value.
	/// </summary>
	public class SearchCriteriaLike : SearchCriteria
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SearchCriteriaLike"/> class.
		/// </summary>
		/// <param name="filterField">The filter field.</param>
		/// <param name="filterValue">The filter value.</param>
		public SearchCriteriaLike(EmailActivitiesFilterField filterField, object filterValue)
			: base(filterField, SearchConditionOperator.Like, filterValue)
		{
		}

		/// <summary>
		/// Converts the filter operator into a string as expected by the SendGrid segmenting API.
		/// </summary>
		/// <returns>The string representation of the operator.</returns>
		public override string ConvertOperatorToString()
		{
			return $" {base.ConvertOperatorToString()} ";
		}
	}
}
