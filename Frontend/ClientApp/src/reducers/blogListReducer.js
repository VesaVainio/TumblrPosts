
import initialState from "./initialState";
import { BLOG_LIST_LOADED } from "../actions/actionTypes";

export default function blogs(state = initialState, action) {
  switch (action.type) {
    case BLOG_LIST_LOADED:
      console.log("BLOG_LIST_LOADED Action");
      var newState = Object.assign({},
        state,
        {
          blogs: action.blogs
        });
      return newState;
    default:
      return state;
  }
}