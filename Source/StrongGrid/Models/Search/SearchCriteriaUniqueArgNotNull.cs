﻿namespace StrongGrid.Models.Search
{
	/// <summary>
	/// Filter the result of a search on the value of a custom tracking argument to be NULL
	/// </summary>
	public class SearchCriteriaUniqueArgNotNull : SearchCriteriaUniqueArg
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SearchCriteriaUniqueArgNotNull"/> class.
		/// </summary>
		/// <param name="uniqueArgName">The name of the unique arg</param>
		public SearchCriteriaUniqueArgNotNull(string uniqueArgName)
			: base(uniqueArgName, SearchConditionOperator.NotNull, null)
		{
		}

		/// <summary>
		/// Converts the filter operator into a string as expected by the SendGrid Email Activities API.
		/// </summary>
		/// <returns>The string representation of the operator</returns>
		public override string ConvertOperatorToString()
		{
			return $" {base.ConvertOperatorToString()}";
		}
	}
}
