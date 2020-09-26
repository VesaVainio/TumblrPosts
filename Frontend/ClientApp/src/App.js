import React, { Component } from 'react';
import { Route } from 'react-router';
import { Layout } from './components/Layout';
import { Home } from './components/Home';
import { Posts } from './components/Posts';
import { Post } from './components/Post';
import Blogs from './components/Blogs';

export default class App extends Component {
  displayName = App.name

  render() {
    return (
      <Layout>
        <Route exact path='/' component={Home} />
        <Route path='/posts/:blogname/:after?' component={Posts} />
        <Route path='/post/:blogname/:postid' component={Post} />
        <Route path='/blogs' component={Blogs} />
      </Layout>
    );
  }
}
