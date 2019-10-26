import 'bootstrap/dist/css/bootstrap.min.css';
import 'jquery';
import 'popper.js';
import 'bootstrap/dist/js/bootstrap.bundle.min';

import React from "react";
import ReactDOM from "react-dom";
import { Provider as ReduxProvider } from "react-redux";
import { HashRouter } from "react-router-dom";
import configureStore from './store/configureStore';
import App from "./App";

const rootElement = document.getElementById("root");

const store = configureStore();

ReactDOM.render(
  <HashRouter>
    <ReduxProvider store={store}>
      <App />
    </ReduxProvider>
  </HashRouter>,
  rootElement);

