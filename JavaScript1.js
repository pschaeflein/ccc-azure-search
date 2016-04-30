if (searchTerm != null) {
	$.ajax({
		dataType: 'json',
		url: 'https://[name].search.windows.net/indexes/posts/docs?api-version=2015-02-28&highlight=content&highlightPreTag=%3Cspan style="background:yellow;"%3E&highlightPostTag=%3c/span%3E&search=' + searchTerm,
		beforeSend: function (xhr) { xhr.setRequestHeader('api-key', '[api-key]') },
		type: 'GET'
	}).success(renderSearchResults);
}
