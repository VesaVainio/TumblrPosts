import { combineReducers } from "redux";
import blogs from "./blogListReducer";

const rootReducer = combineReducers({
  blogs
});

export default rootReducer;