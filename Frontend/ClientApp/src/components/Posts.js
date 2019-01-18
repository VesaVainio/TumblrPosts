import React, { Component } from 'react';
import StackGrid from "react-stack-grid";
import Utils from "../Utils";

export class Posts extends Component {
  displayName = Posts.name

  constructor(props) {
    super(props);
    this.state = { posts: [], loading: true };

    this.handleClick = this.handleClick.bind(this);

    fetch(process.env.REACT_APP_API_ROOT + '/api/posts/' + props.match.params.blogname)
    .then(response => response.json())
    .then(data => {
      this.setState({ posts: data, loading: false });
    });
  }
  
  handleClick(e) {
    var blogname = this.props.match.params.blogname;
    var id = e.target.parentElement.getAttribute('data-id');
    this.props.history.push('/posts/' + blogname + "/" + id);
  }

  static renderPostsTable(posts) {
    return (
      <StackGrid columnWidth={250} monitorImagesLoaded={true}>
        {posts.map(post =>
          <div data-grid={{ static: true }}>
            {!post.Photos || post.Photos.length === 0 &&
              <span>No photo</span>
            }
            {post.Photos && post.Photos.length !== 0 &&
              <div className="photo-post"><a href={"/post/" + post.Blogname + "/" + post.Id}>
                <img src={Utils.GetSmallPhotoUrl(post)} width="250" data-id={post.Id} onLoad={this.imageReady} onError={this.imageReady} alt="" />
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
      </div>
    );
  }
}
