import React, { Component } from 'react';
import StackGrid from "react-stack-grid";
import Utils from "../Utils";

export class FetchData extends Component {
  displayName = FetchData.name

  constructor(props) {
    super(props);
    this.state = { forecasts: [], loading: true };

      fetch(process.env.REACT_APP_API_ROOT + '/api/posts/' + process.env.REACT_APP_DEFAULT_BLOG)
      .then(response => response.json())
      .then(data => {
        this.setState({ forecasts: data, loading: false });
      });
  }

  static renderPostsTable(posts) {
    return (
      <StackGrid columnWidth={250}>
        {posts.map(post =>
          <div key={post.Id}>
            {!post.Photos && 
              <span>No photo</span>
            }
            {post.Photos &&
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
        : FetchData.renderPostsTable(this.state.forecasts);

    return (
      <div>
        <h1>Weather forecast</h1>
        <p>This component demonstrates fetching data from the server.</p>
        {contents}
      </div>
    );
  }
}
