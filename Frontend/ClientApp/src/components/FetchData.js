import React, { Component } from 'react';

export class FetchData extends Component {
  displayName = FetchData.name

  constructor(props) {
    super(props);
    this.state = { forecasts: [], loading: true };

      fetch(process.env.REACT_APP_API_ROOT + '/api/posts/teasefantasies')
      .then(response => response.json())
      .then(data => {
        this.setState({ forecasts: data, loading: false });
      });
  }

  static renderPostsTable(posts) {
    return (
      <table className='table'>
        <thead>
          <tr>
            <th>Blogname</th>
            <th>Id</th>
            <th>Type</th>
            <th>Date</th>
          </tr>
        </thead>
        <tbody>
          {posts.map(post =>
            <tr key={post.Id}>
              <td>{post.Blogname}</td>
              <td>{post.Id}</td>
              <td>{post.Type}</td>
              <td>{post.Date}</td>
            </tr>
          )}
        </tbody>
      </table>
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
