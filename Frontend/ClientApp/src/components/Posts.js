import React, { Component } from 'react';
import StackGrid from "react-stack-grid";
import Waypoint from 'react-waypoint';
import Utils from "../Utils";

export class Posts extends Component {
  displayName = Posts.name

  constructor(props) {
    super(props);
    this.state = { posts: [], loading: true };

    this.loadMore = this.loadMore.bind(this);

    fetch(process.env.REACT_APP_API_ROOT + '/api/posts/' + props.match.params.blogname)
    .then(response => response.json())
    .then(data => {
      this.setState({ posts: data, loading: false, hasMore: data.length === 50 });
    });
  }

  loadMore() {
    if (!this.state.hasMore) {
      return;
    }

    const [lastPost] = this.state.posts.slice(-1);
    fetch(process.env.REACT_APP_API_ROOT + '/api/posts/' + this.props.match.params.blogname + "?after=" + lastPost.Id)
      .then(response => response.json())
      .then(data => {
        this.setState(state => ({
          posts: state.posts.concat(data),
          hasMore: data.length === 50 
        }));
      });
  }

  static renderPostsTable(posts) {
    return (
      <StackGrid columnWidth={250} monitorImagesLoaded={true}>
        {posts.map(post =>
          <div key={post.Id}>
            {(!post.Photos || post.Photos.length === 0) &&
              <span>No photo</span>
            }
            {(post.Photos && post.Photos.length !== 0) &&
              <div className="photo-post"><a href={"/post/" + post.Blogname + "/" + post.Id}>
                <img src={Utils.GetBigPhotoUrl(post)} width="250" alt="" />
              </a></div>
            }
          </div>
        )}
      </StackGrid>
    );
  }

  render() {
    let contents = this.state.loading
      ? <p><em>Loading...</em></p>
        : Posts.renderPostsTable(this.state.posts);

    return (
      <div>
        <h1>{this.props.match.params.blogname}</h1>
        {contents}
        <Waypoint onEnter={this.loadMore}>
          <div>
            Some content here
          </div>
        </Waypoint>
      </div>
    );
  }
}
