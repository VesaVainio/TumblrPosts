import React, { Component } from 'react';
import StackGrid from "react-stack-grid";
import Utils from "../Utils";

export class Posts extends Component {
  displayName = Posts.name

  constructor(props) {
    super(props);
    this.state = { forecasts: [], loading: true };

    fetch(process.env.REACT_APP_API_ROOT + '/api/posts/' + props.match.params.blogname)
    .then(response => response.json())
    .then(data => {
      this.setState({ forecasts: data, loading: false });
    });
  }

  static renderPostsTable(posts) {
    return (
      <StackGrid columnWidth={250} monitorImagesLoaded={true}>
        {posts.map(post =>
          <div key={post.Id}>
            {!post.Photos || post.Photos.length === 0 && 
              <span>No photo</span>
            }
            {post.Photos && post.Photos.length !== 0 &&
              <img src={Utils.GetPhotoUrl(post)} width="250"/>
            }
          </div>
        )}
      </StackGrid>
    );
  }

  render() {
    let contents = this.state.loading
      ? <p><em>Loading...</em></p>
        : Posts.renderPostsTable(this.state.forecasts);

    return (
      <div>
        <h1>{this.props.match.params.blogname}</h1>
        {contents}
      </div>
    );
  }
}
