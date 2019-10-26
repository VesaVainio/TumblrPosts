import * as types from "./actionTypes";

export function blogListLoaded(blogs) {
  return { type: types.BLOG_LIST_LOADED, blogs: blogs };
}

export function loadBlogList() {
  return dispatch => {
    fetch(process.env.REACT_APP_API_ROOT + '/api/blogs')
      .then(response => response.json())
      .then(data => dispatch(blogListLoaded(data)));
  }
};
