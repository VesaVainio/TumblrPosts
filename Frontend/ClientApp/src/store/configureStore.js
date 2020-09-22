import { createStore, applyMiddleware } from "redux";
import rootReducer from "../reducers/rootReducer";
import thunk from "redux-thunk";
import history from './history';
import { routerMiddleware } from 'react-router-redux';

const routingMiddleware = routerMiddleware(history);

export default function configureStore() {
  return createStore(
    rootReducer,
    window.__REDUX_DEVTOOLS_EXTENSION__ && window.__REDUX_DEVTOOLS_EXTENSION__(),
    applyMiddleware(routingMiddleware, thunk)
  );
}